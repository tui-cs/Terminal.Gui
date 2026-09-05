using System.Text;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Terminal.Gui.KeySequences;

/// <summary>Provides leader-key sequence bindings.</summary>
public sealed class KeySequenceBindings
{
    private readonly List<KeySequenceBinding> _bindings = [];
    private readonly List<Key> _keys = [];
    private readonly HashSet<Key> _leaders = new (new KeyEqualityComparer ());

    private Key? _leaderKey;
    private DateTimeOffset _lastKeyTime;
    private bool _isCommandMode;

    /// <summary>Raised when sequence capture state changes.</summary>
    public event EventHandler<KeySequenceStateChangedEventArgs>? StateChanged;

    /// <summary>Gets or sets the capture timeout.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds (1);

    /// <summary>Gets or sets the key that cancels active sequence capture.</summary>
    public Key CancelKey { get; set; } = Key.Esc;

    /// <summary>Gets or sets the matching mode.</summary>
    public KeySequenceMode Mode { get; set; }

    /// <summary>Gets or sets the key that enters persistent command mode.</summary>
    public Key EnterModeKey { get; set; } = Key.Esc;

    /// <summary>Gets or sets the key that exits persistent command mode.</summary>
    public Key ExitModeKey { get; set; } = 'i';

    /// <summary>Gets or sets the time provider used for timeout checks.</summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>Gets whether sequence capture is active.</summary>
    public bool IsCapturing => _leaderKey is { } || _isCommandMode;

    /// <summary>Gets whether persistent command mode is active.</summary>
    public bool IsCommandMode => _isCommandMode;

    /// <summary>Adds a leader key.</summary>
    public void AddLeader (Key leaderKey)
    {
        if (!leaderKey.IsValid)
        {
            throw new ArgumentException (@"Leader key must be valid.", nameof (leaderKey));
        }

        _leaders.Add (leaderKey);
    }

    /// <summary>Removes a leader key.</summary>
    public void RemoveLeader (Key leaderKey) => _leaders.Remove (leaderKey);

    /// <summary>Determines whether the specified key is a leader key.</summary>
    public bool IsLeader (Key key) => _leaders.Contains (key);

    /// <summary>Adds a sequence binding.</summary>
    public void Add (KeySequencePattern pattern, KeySequenceHandler handler)
    {
        ArgumentNullException.ThrowIfNull (pattern);
        ArgumentNullException.ThrowIfNull (handler);

        if (pattern.LeaderKey is { } leaderKey && !leaderKey.IsValid)
        {
            throw new ArgumentException (@"Pattern must have a valid leader key.", nameof (pattern));
        }

        if (pattern.Tokens.Count == 0)
        {
            throw new ArgumentException (@"Pattern must contain at least one token after the leader.", nameof (pattern));
        }

        if (pattern.Tokens.Count (t => t.Kind == KeySequenceTokenKind.Count) > 1)
        {
            throw new ArgumentException (@"Pattern can contain only one count token.", nameof (pattern));
        }

        string patternText = pattern.ToString ();

        if (_bindings.Any (b => b.Pattern.ToString () == patternText))
        {
            throw new InvalidOperationException ($"A binding for {patternText} already exists.");
        }

        if (pattern.LeaderKey is { } patternLeaderKey)
        {
            AddLeader (patternLeaderKey);
        }

        _bindings.Add (new KeySequenceBinding (pattern, handler));
    }

    /// <summary>Adds a sequence binding from a compact pattern string.</summary>
    public void Add (string pattern, KeySequenceHandler handler) => Add (KeySequenceParser.Parse (pattern), handler);

    /// <summary>Adds a persistent command-mode sequence binding from a compact pattern string.</summary>
    public void AddMode (string pattern, KeySequenceHandler handler) => Add (KeySequenceParser.ParseCommandMode (pattern), handler);

    /// <summary>Removes a sequence binding.</summary>
    public bool Remove (KeySequencePattern pattern)
    {
        KeySequenceBinding? binding = _bindings.FirstOrDefault (b => b.Pattern.ToString () == pattern.ToString ());

        if (binding is null)
        {
            return false;
        }

        _bindings.Remove (binding);
        return true;
    }

    /// <summary>Removes all sequence bindings and leaders.</summary>
    public void Clear ()
    {
        _bindings.Clear ();
        _leaders.Clear ();
        Reset ();
    }

    /// <summary>Processes a key for a target view.</summary>
    public KeySequenceResult ProcessKey (View target, Key key, CommandContext? commandContext = null)
    {
        ArgumentNullException.ThrowIfNull (target);
        ArgumentNullException.ThrowIfNull (key);

        KeySequenceResult timeoutResult = ResetIfTimedOut ();

        if (timeoutResult == KeySequenceResult.TimedOut && Mode != KeySequenceMode.Persistent && !IsLeader (key))
        {
            return KeySequenceResult.TimedOut;
        }

        if (Mode == KeySequenceMode.Persistent)
        {
            return ProcessPersistentKey (target, key, commandContext);
        }

        if (_leaderKey is null)
        {
            if (!IsLeader (key))
            {
                return KeySequenceResult.NotLeader;
            }

            _leaderKey = key;
            _keys.Clear ();
            _lastKeyTime = TimeProvider.GetUtcNow ();
            RaiseStateChanged (KeySequenceResult.Started, CandidateCount ());
            return KeySequenceResult.Started;
        }

        if (!key.IsValid || key.IsModifierOnly)
        {
            Reset ();
            RaiseStateChanged (KeySequenceResult.Rejected, 0);
            return KeySequenceResult.Rejected;
        }

        if (key == CancelKey)
        {
            Reset ();
            RaiseStateChanged (KeySequenceResult.Canceled, 0);
            return KeySequenceResult.Canceled;
        }

        _keys.Add (key);
        _lastKeyTime = TimeProvider.GetUtcNow ();

        MatchEvaluation evaluation = Evaluate (target, commandContext);

        if (evaluation.Result != KeySequenceResult.Pending)
        {
            Reset ();
        }

        RaiseStateChanged (evaluation.Result, evaluation.CandidateCount);
        return evaluation.Result;
    }

    /// <summary>Resets active sequence capture.</summary>
    public void Reset ()
    {
        _leaderKey = null;
        _keys.Clear ();
    }

    /// <summary>Enters persistent command mode.</summary>
    public void EnterCommandMode ()
    {
        _isCommandMode = true;
        Reset ();
        RaiseStateChanged (KeySequenceResult.ModeEntered, CandidateCount ());
    }

    /// <summary>Exits persistent command mode.</summary>
    public void ExitCommandMode ()
    {
        _isCommandMode = false;
        Reset ();
        RaiseStateChanged (KeySequenceResult.ModeExited, 0);
    }

    private KeySequenceResult ResetIfTimedOut ()
    {
        if ((!IsCapturing || _keys.Count == 0) || Timeout <= TimeSpan.Zero)
        {
            return KeySequenceResult.NotLeader;
        }

        if (TimeProvider.GetUtcNow () - _lastKeyTime <= Timeout)
        {
            return KeySequenceResult.NotLeader;
        }

        Reset ();
        RaiseStateChanged (KeySequenceResult.TimedOut, 0);
        return KeySequenceResult.TimedOut;
    }

    private KeySequenceResult ProcessPersistentKey (View target, Key key, CommandContext? commandContext)
    {
        if (!_isCommandMode)
        {
            if (key != EnterModeKey)
            {
                return KeySequenceResult.NotLeader;
            }

            EnterCommandMode ();
            return KeySequenceResult.ModeEntered;
        }

        if (key == ExitModeKey)
        {
            ExitCommandMode ();
            return KeySequenceResult.ModeExited;
        }

        if (!key.IsValid || key.IsModifierOnly)
        {
            Reset ();
            RaiseStateChanged (KeySequenceResult.Rejected, CandidateCount ());
            return KeySequenceResult.Rejected;
        }

        if (key == CancelKey)
        {
            Reset ();
            RaiseStateChanged (KeySequenceResult.Canceled, CandidateCount ());
            return KeySequenceResult.Canceled;
        }

        _keys.Add (key);
        _lastKeyTime = TimeProvider.GetUtcNow ();

        MatchEvaluation evaluation = Evaluate (target, commandContext);

        if (evaluation.Result != KeySequenceResult.Pending)
        {
            Reset ();
        }

        RaiseStateChanged (evaluation.Result, evaluation.CandidateCount);
        return evaluation.Result;
    }

    private MatchEvaluation Evaluate (View target, CommandContext? commandContext)
    {
        List<CandidateMatch> matches = [];
        int candidateCount = 0;

        foreach (KeySequenceBinding sequenceBinding in GetCandidateBindings ())
        {
            CandidateMatch match = KeySequenceMatcher.Match (sequenceBinding.Pattern, _keys);

            if (match.Kind == CandidateMatchKind.NoMatch)
            {
                continue;
            }

            candidateCount++;
            matches.Add (match with { Binding = sequenceBinding });
        }

        CandidateMatch? complete = matches.FirstOrDefault (m => m.Kind == CandidateMatchKind.Complete);

        if (complete is { Binding: { } binding })
        {
            if (complete.Count == 0 && !binding.Pattern.AllowZeroCount)
            {
                return new MatchEvaluation (KeySequenceResult.Rejected, candidateCount);
            }

            bool hasLongerCandidate = matches.Any (m => m.Kind == CandidateMatchKind.Prefix && m.Binding != binding);

            if (hasLongerCandidate && binding.Pattern.MatchMode == KeySequenceMatchMode.Longest)
            {
                return new MatchEvaluation (KeySequenceResult.Pending, candidateCount);
            }

            KeySequenceContext context = CreateContext (target, binding.Pattern, complete, commandContext);
            bool handled = binding.Handler (context);

            return new MatchEvaluation (handled ? KeySequenceResult.Matched : KeySequenceResult.Rejected, candidateCount);
        }

        if (matches.Any (m => m.Kind == CandidateMatchKind.Prefix))
        {
            return new MatchEvaluation (KeySequenceResult.Pending, candidateCount);
        }

        return new MatchEvaluation (KeySequenceResult.Rejected, 0);
    }

    private IEnumerable<KeySequenceBinding> GetCandidateBindings ()
    {
        if (Mode == KeySequenceMode.Persistent && _isCommandMode)
        {
            return _bindings.Where (b => b.Pattern.LeaderKey is null);
        }

        if (_leaderKey is null)
        {
            return [];
        }

        Key activeLeader = _leaderKey;

        return _bindings.Where (b => b.Pattern.LeaderKey is { } patternLeader && patternLeader == activeLeader);
    }

    private KeySequenceContext CreateContext (View target, KeySequencePattern pattern, CandidateMatch match, CommandContext? commandContext)
    {
        List<Key> literalKeys = pattern.Tokens.Where (t => t.Kind == KeySequenceTokenKind.Literal && t.Key is { }).Select (t => t.Key!).ToList ();
        Dictionary<string, object?> values = new (match.Values);

        return new KeySequenceContext
        {
            Target = target,
            LeaderKey = _leaderKey,
            IsCommandMode = Mode == KeySequenceMode.Persistent && _isCommandMode,
            Keys = _keys.ToArray (),
            Pattern = pattern,
            Count = match.Count,
            OperatorKey = literalKeys.FirstOrDefault (),
            MotionKey = literalKeys.LastOrDefault (),
            Values = values,
            CommandContext = commandContext
        };
    }

    private int CandidateCount ()
    {
        if (Mode == KeySequenceMode.Persistent && _isCommandMode)
        {
            return _bindings.Count (b => b.Pattern.LeaderKey is null);
        }

        if (_leaderKey is null)
        {
            return 0;
        }

        Key activeLeader = _leaderKey;

        return _bindings.Count (b => b.Pattern.LeaderKey is { } patternLeader && patternLeader == activeLeader);
    }

    private void RaiseStateChanged (KeySequenceResult result, int candidateCount)
    {
        string countText = GetCountText ();
        KeySequenceState state = _isCommandMode ? KeySequenceState.CommandMode : IsCapturing ? KeySequenceState.Capturing : KeySequenceState.Idle;
        StateChanged?.Invoke (this, new KeySequenceStateChangedEventArgs (state, _leaderKey, _keys.ToArray (), countText, candidateCount, result, _isCommandMode));
    }

    private string GetCountText ()
    {
        StringBuilder builder = new ();

        foreach (Key key in _keys)
        {
            if (!KeySequenceMatcher.IsDigit (key, out char digit))
            {
                continue;
            }

            builder.Append (digit);
        }

        return builder.ToString ();
    }
}
