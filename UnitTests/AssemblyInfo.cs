using System;
using System.Diagnostics;
using System.Reflection;
using Terminal.Gui;
using Xunit;

// Since Application is a singleton we can't run tests in parallel
[assembly: CollectionBehavior (DisableTestParallelization = true)]

