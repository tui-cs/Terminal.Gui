// This code is adapted from https://github.com/devblackops/Terminal-Icons (which also uses the MIT license).
// Glyph mappings: https://github.com/ryanoasis/nerd-fonts/blob/v3.5.1/glyphnames.json
// Nerd fonts can be installed by following the instructions on the Nerd Fonts repository: https://github.com/ryanoasis/nerd-fonts

using System.IO.Abstractions;

namespace Terminal.Gui.Text;

internal class NerdFonts
{
    private readonly Rune _nf_cod_file = new (0xEA7B);
    private readonly Rune _nf_cod_folder = new (0xEA83);
    private readonly Rune _nf_cod_folder_opened = new (0xEAF7);

    /// <summary>
    ///     If <see langword="true"/>, enables the use of Nerd unicode symbols. This requires specific font(s) to be
    ///     installed on the users machine to work correctly.  Defaults to <see langword="false"/>.
    /// </summary>
    public static bool Enable => NerdFontsSettings.Current.Enable;

    /// <summary>Mapping of file extension to <see cref="Glyphs"/> name.</summary>
    /// <remarks>Keys are case-insensitive by default. The longest matching extension takes precedence.</remarks>
    public Dictionary<string, string> ExtensionToIcon { get; set; } = new (StringComparer.OrdinalIgnoreCase)
    {
        // Archive files
        { ".7z", "nf-oct-file_zip" },
        { ".bz", "nf-oct-file_zip" },
        { ".tar", "nf-oct-file_zip" },
        { ".zip", "nf-oct-file_zip" },
        { ".gz", "nf-oct-file_zip" },
        { ".xz", "nf-oct-file_zip" },
        { ".br", "nf-oct-file_zip" },
        { ".bzip2", "nf-oct-file_zip" },
        { ".gzip", "nf-oct-file_zip" },
        { ".brotli", "nf-oct-file_zip" },
        { ".rar", "nf-oct-file_zip" },
        { ".tgz", "nf-oct-file_zip" },

        // Executable things
        { ".bat", "nf-custom-msdos" },
        { ".cmd", "nf-custom-msdos" },
        { ".exe", "nf-md-application" },
        { ".pl", "nf-dev-perl" },
        { ".sh", "nf-oct-terminal" },

        // App Packages
        { ".msi", "nf-md-package_variant" },
        { ".msix", "nf-md-package_variant" },
        { ".msixbundle", "nf-md-package_variant" },
        { ".appx", "nf-md-package_variant" },
        { ".AppxBundle", "nf-md-package_variant" },
        { ".deb", "nf-md-package_variant" },
        { ".rpm", "nf-md-package_variant" },

        // PowerShell
        { ".ps1", "nf-md-console_line" },
        { ".psm1", "nf-md-console_line" },
        { ".psd1", "nf-md-console_line" },
        { ".ps1xml", "nf-md-console_line" },
        { ".psc1", "nf-md-console_line" },
        { ".pssc", "nf-md-console_line" },

        // Javascript
        { ".js", "nf-dev-javascript" },
        { ".esx", "nf-dev-javascript" },
        { ".mjs", "nf-dev-javascript" },

        // Java
        { ".java", "nf-fae-java" },
        { ".jar", "nf-fae-java" },
        { ".gradle", "nf-md-elephant" },

        // Python
        { ".py", "nf-dev-python" },
        { ".ipynb", "nf-md-notebook" },

        // React
        { ".jsx", "nf-dev-react" },
        { ".tsx", "nf-dev-react" },

        // Typescript
        { ".ts", "nf-seti-typescript" },

        // Not-executable code files
        { ".dll", "nf-fa-archive" },

        // Importable Data files
        { ".clixml", "nf-md-code_tags" },
        { ".csv", "nf-md-file_excel" },
        { ".tsv", "nf-md-file_excel" },

        // Settings
        { ".ini", "nf-fa-gear" },
        { ".dlc", "nf-fa-gear" },
        { ".config", "nf-fa-gear" },
        { ".conf", "nf-fa-gear" },
        { ".properties", "nf-fa-gear" },
        { ".prop", "nf-fa-gear" },
        { ".settings", "nf-fa-gear" },
        { ".option", "nf-fa-gear" },
        { ".reg", "nf-fa-gear" },
        { ".props", "nf-fa-gear" },
        { ".toml", "nf-fa-gear" },
        { ".prefs", "nf-fa-gear" },
        { ".sln.dotsettings", "nf-fa-gear" },
        { ".sln.dotsettings.user", "nf-fa-gear" },
        { ".cfg", "nf-fa-gear" },

        // Source Files
        { ".c", "nf-md-language_c" },
        { ".cpp", "nf-md-language_cpp" },
        { ".go", "nf-dev-go" },
        { ".php", "nf-dev-php" },

        // Visual Studio
        { ".csproj", "nf-dev-visualstudio" },
        { ".ruleset", "nf-dev-visualstudio" },
        { ".sln", "nf-dev-visualstudio" },
        { ".slnf", "nf-dev-visualstudio" },
        { ".suo", "nf-dev-visualstudio" },
        { ".vb", "nf-dev-visualstudio" },
        { ".vbs", "nf-dev-visualstudio" },
        { ".vcxitems", "nf-dev-visualstudio" },
        { ".vcxitems.filters", "nf-dev-visualstudio" },
        { ".vcxproj", "nf-dev-visualstudio" },
        { ".vsxproj.filters", "nf-dev-visualstudio" },

        // CSharp
        { ".cs", "nf-md-language_csharp" },
        { ".csx", "nf-md-language_csharp" },

        // Haskell
        { ".hs", "nf-dev-haskell" },

        // XAML
        { ".xaml", "nf-md-language_xaml" },

        // Rust
        { ".rs", "nf-dev-rust" },

        // Database
        { ".pdb", "nf-dev-database" },
        { ".sql", "nf-dev-database" },
        { ".pks", "nf-dev-database" },
        { ".pkb", "nf-dev-database" },
        { ".accdb", "nf-dev-database" },
        { ".mdb", "nf-dev-database" },
        { ".sqlite", "nf-dev-database" },
        { ".pgsql", "nf-dev-database" },
        { ".postgres", "nf-dev-database" },
        { ".psql", "nf-dev-database" },

        // Source Control
        { ".patch", "nf-dev-git" },

        // Project files
        { ".user", "nf-md-microsoft_visual_studio" },
        { ".code-workspace", "nf-md-microsoft_visual_studio" },

        // Text data files
        { ".log", "nf-fa-list" },
        { ".txt", "nf-md-file_document" },

        // Subtitle files
        { ".srt", "nf-md-file_document" },
        { ".lrc", "nf-md-file_document" },
        { ".ass", "nf-fa-eye" },

        // HTML/css
        { ".html", "nf-seti-html" },
        { ".htm", "nf-seti-html" },
        { ".xhtml", "nf-seti-html" },
        { ".html_vm", "nf-seti-html" },
        { ".asp", "nf-seti-html" },
        { ".css", "nf-dev-css3" },
        { ".sass", "nf-dev-sass" },
        { ".scss", "nf-dev-sass" },
        { ".less", "nf-dev-less" },

        // Markdown
        { ".md", "nf-dev-markdown" },
        { ".markdown", "nf-dev-markdown" },
        { ".rst", "nf-dev-markdown" },

        // Handlebars
        { ".hbs", "nf-seti-mustache" },

        // JSON
        { ".json", "nf-seti-json" },
        { ".tsbuildinfo", "nf-seti-json" },

        // YAML
        { ".yml", "nf-md-format_align_left" },
        { ".yaml", "nf-md-format_align_left" },

        // LUA
        { ".lua", "nf-seti-lua" },

        // Clojure
        { ".clj", "nf-dev-clojure" },
        { ".cljs", "nf-dev-clojure" },
        { ".cljc", "nf-dev-clojure" },

        // Groovy
        { ".groovy", "nf-dev-groovy" },

        // Vue
        { ".vue", "nf-md-vuejs" },

        // Dart
        { ".dart", "nf-dev-dart" },

        // Elixir
        { ".ex", "nf-custom-elixir" },
        { ".exs", "nf-custom-elixir" },
        { ".eex", "nf-custom-elixir" },
        { ".leex", "nf-custom-elixir" },

        // Erlang
        { ".erl", "nf-dev-erlang" },

        // Elm
        { ".elm", "nf-custom-elm" },

        // Applescript
        { ".applescript", "nf-dev-apple" },

        // XML
        { ".xml", "nf-md-xml" },
        { ".plist", "nf-md-xml" },
        { ".xsd", "nf-md-xml" },
        { ".dtd", "nf-md-xml" },
        { ".xsl", "nf-md-xml" },
        { ".xslt", "nf-md-xml" },
        { ".resx", "nf-md-xml" },
        { ".iml", "nf-md-xml" },
        { ".xquery", "nf-md-xml" },
        { ".tmLanguage", "nf-md-xml" },
        { ".manifest", "nf-md-xml" },
        { ".project", "nf-md-xml" },

        // Documents
        { ".chm", "nf-md-help_box" },
        { ".pdf", "nf-md-file_pdf_box" },

        // Excel
        { ".xls", "nf-md-file_excel" },
        { ".xlsx", "nf-md-file_excel" },

        // PowerPoint
        { ".pptx", "nf-md-file_powerpoint" },
        { ".ppt", "nf-md-file_powerpoint" },
        { ".pptm", "nf-md-file_powerpoint" },
        { ".potx", "nf-md-file_powerpoint" },
        { ".potm", "nf-md-file_powerpoint" },
        { ".ppsx", "nf-md-file_powerpoint" },
        { ".ppsm", "nf-md-file_powerpoint" },
        { ".pps", "nf-md-file_powerpoint" },
        { ".ppam", "nf-md-file_powerpoint" },
        { ".ppa", "nf-md-file_powerpoint" },

        // Word
        { ".doc", "nf-md-file_word" },
        { ".docx", "nf-md-file_word" },
        { ".rtf", "nf-md-file_word" },

        // Audio
        { ".mp3", "nf-fa-file_audio_o" },
        { ".flac", "nf-fa-file_audio_o" },
        { ".m4a", "nf-fa-file_audio_o" },
        { ".wma", "nf-fa-file_audio_o" },
        { ".aiff", "nf-fa-file_audio_o" },
        { ".wav", "nf-fa-file_audio_o" },
        { ".aac", "nf-fa-file_audio_o" },
        { ".opus", "nf-fa-file_audio_o" },

        // Images
        { ".png", "nf-fa-file_image_o" },
        { ".jpeg", "nf-fa-file_image_o" },
        { ".jpg", "nf-fa-file_image_o" },
        { ".gif", "nf-fa-file_image_o" },
        { ".ico", "nf-fa-file_image_o" },
        { ".tif", "nf-fa-file_image_o" },
        { ".tiff", "nf-fa-file_image_o" },
        { ".psd", "nf-fa-file_image_o" },
        { ".psb", "nf-fa-file_image_o" },
        { ".ami", "nf-fa-file_image_o" },
        { ".apx", "nf-fa-file_image_o" },
        { ".bmp", "nf-fa-file_image_o" },
        { ".bpg", "nf-fa-file_image_o" },
        { ".brk", "nf-fa-file_image_o" },
        { ".cur", "nf-fa-file_image_o" },
        { ".dds", "nf-fa-file_image_o" },
        { ".dng", "nf-fa-file_image_o" },
        { ".eps", "nf-fa-file_image_o" },
        { ".exr", "nf-fa-file_image_o" },
        { ".fpx", "nf-fa-file_image_o" },
        { ".gbr", "nf-fa-file_image_o" },
        { ".jbig2", "nf-fa-file_image_o" },
        { ".jb2", "nf-fa-file_image_o" },
        { ".jng", "nf-fa-file_image_o" },
        { ".jxr", "nf-fa-file_image_o" },
        { ".pbm", "nf-fa-file_image_o" },
        { ".pgf", "nf-fa-file_image_o" },
        { ".pic", "nf-fa-file_image_o" },
        { ".raw", "nf-fa-file_image_o" },
        { ".webp", "nf-fa-file_image_o" },
        { ".svg", "nf-md-svg" },

        // Video
        { ".webm", "nf-fa-file_video_o" },
        { ".mkv", "nf-fa-file_video_o" },
        { ".flv", "nf-fa-file_video_o" },
        { ".vob", "nf-fa-file_video_o" },
        { ".ogv", "nf-fa-file_video_o" },
        { ".ogg", "nf-fa-file_video_o" },
        { ".gifv", "nf-fa-file_video_o" },
        { ".avi", "nf-fa-file_video_o" },
        { ".mov", "nf-fa-file_video_o" },
        { ".qt", "nf-fa-file_video_o" },
        { ".wmv", "nf-fa-file_video_o" },
        { ".yuv", "nf-fa-file_video_o" },
        { ".rm", "nf-fa-file_video_o" },
        { ".rmvb", "nf-fa-file_video_o" },
        { ".mp4", "nf-fa-file_video_o" },
        { ".mpg", "nf-fa-file_video_o" },
        { ".mp2", "nf-fa-file_video_o" },
        { ".mpeg", "nf-fa-file_video_o" },
        { ".mpe", "nf-fa-file_video_o" },
        { ".mpv", "nf-fa-file_video_o" },
        { ".m2v", "nf-fa-file_video_o" },

        // Email
        { ".ics", "nf-fa-calendar" },

        // Certificates
        { ".cer", "nf-fa-certificate" },
        { ".cert", "nf-fa-certificate" },
        { ".crt", "nf-fa-certificate" },
        { ".pfx", "nf-fa-certificate" },

        // Keys
        { ".pem", "nf-fa-key" },
        { ".pub", "nf-fa-key" },
        { ".key", "nf-fa-key" },
        { ".asc", "nf-fa-key" },
        { ".gpg", "nf-fa-key" },

        // Fonts
        { ".woff", "nf-fa-font" },
        { ".woff2", "nf-fa-font" },
        { ".ttf", "nf-fa-font" },
        { ".eot", "nf-fa-font" },
        { ".suit", "nf-fa-font" },
        { ".otf", "nf-fa-font" },
        { ".bmap", "nf-fa-font" },
        { ".fnt", "nf-fa-font" },
        { ".odttf", "nf-fa-font" },
        { ".ttc", "nf-fa-font" },
        { ".font", "nf-fa-font" },
        { ".fonts", "nf-fa-font" },
        { ".sui", "nf-fa-font" },
        { ".ntf", "nf-fa-font" },
        { ".mrg", "nf-fa-font" },

        // Ruby
        { ".rb", "nf-oct-ruby" },
        { ".erb", "nf-oct-ruby" },
        { ".gemfile", "nf-oct-ruby" },
        { "rakefile", "nf-oct-ruby" },

        // FSharp
        { ".fs", "nf-dev-fsharp" },
        { ".fsx", "nf-dev-fsharp" },
        { ".fsi", "nf-dev-fsharp" },
        { ".fsproj", "nf-dev-fsharp" },

        // Docker
        { ".dockerignore", "nf-dev-docker" },
        { ".dockerfile", "nf-dev-docker" },

        // VSCode
        { ".vscodeignore", "nf-fa-gear" },
        { ".vsixmanifest", "nf-fa-gear" },
        { ".vsix", "nf-fa-gear" },
        { ".code-workplace", "nf-fa-gear" },

        // Sublime
        { ".sublime-project", "nf-dev-sublime" },
        { ".sublime-workspace", "nf-dev-sublime" },
        { ".lock", "nf-fa-lock" },

        // Terraform
        { ".tf", "nf-md-code_tags" },
        { ".tfvars", "nf-md-code_tags" },
        { ".tf.json", "nf-md-code_tags" },
        { ".tfvars.json", "nf-md-code_tags" },
        { ".auto.tfvars", "nf-md-code_tags" },
        { ".auto.tfvars.json", "nf-md-code_tags" },

        // Disk Image
        { ".vmdk", "nf-md-harddisk" },
        { ".vhd", "nf-md-harddisk" },
        { ".vhdx", "nf-md-harddisk" },
        { ".img", "nf-fae-disco" },
        { ".iso", "nf-fae-disco" },

        // R language
        { ".R", "nf-md-language_r" },
        { ".Rmd", "nf-md-language_r" },
        { ".Rproj", "nf-md-language_r" },

        // Julia language
        { ".jl", "nf-seti-julia" },

        // Vim
        { ".vim", "nf-custom-vim" },

        // Puppet
        { ".pp", "nf-custom-puppet" },
        { ".epp", "nf-custom-puppet" }
    };

    /// <summary>Mapping of file name to <see cref="Glyphs"/> name.</summary>
    /// <remarks>Keys are case-insensitive by default. File name matches take precedence over extension matches.</remarks>
    public Dictionary<string, string> FilenameToIcon { get; set; } = new (StringComparer.OrdinalIgnoreCase)
    {
        { "docs", "nf-oct-repo" },
        { "documents", "nf-oct-repo" },
        { "desktop", "nf-md-desktop_classic" },
        { "benchmark", "nf-md-timer" },
        { "demo", "nf-cod-preview" },
        { "samples", "nf-cod-preview" },
        { "contacts", "nf-md-contacts" },
        { "apps", "nf-md-apps" },
        { "applications", "nf-md-apps" },
        { "artifacts", "nf-cod-package" },
        { "shortcuts", "nf-oct-file_symlink_directory" },
        { "links", "nf-oct-file_symlink_directory" },
        { "fonts", "nf-fa-font" },
        { "images", "nf-md-folder_image" },
        { "photos", "nf-md-folder_image" },
        { "pictures", "nf-md-folder_image" },
        { "videos", "nf-md-movie" },
        { "movies", "nf-md-movie" },
        { "media", "nf-dev-html5" },
        { "music", "nf-md-music_box_multiple" },
        { "songs", "nf-md-music_box_multiple" },
        { "onedrive", "nf-md-microsoft_onedrive" },
        { "downloads", "nf-md-folder_download" },
        { "src", "nf-oct-terminal" },
        { "development", "nf-oct-terminal" },
        { "projects", "nf-seti-project" },
        { "bin", "nf-oct-file_binary" },
        { "tests", "nf-md-test_tube" },
        { "windows", "nf-fa-windows" },
        { "users", "nf-fa-users" },
        { "favorites", "nf-md-folder_star" },
        { ".config", "nf-seti-config" },
        { ".cache", "nf-md-cached" },
        { ".vscode", "nf-custom-folder_config" },
        { ".vscode-insiders", "nf-custom-folder_config" },
        { ".git", "nf-custom-folder_git" },
        { ".github", "nf-custom-folder_github" },
        { "github", "nf-fa-github_alt" },
        { "node_modules", "nf-custom-folder_npm" },
        { ".azure", "nf-md-microsoft_azure" },
        { ".aws", "nf-dev-aws" },
        { ".kube", "nf-md-ship_wheel" },
        { ".docker", "nf-dev-docker" },
        { "umbraco", "nf-md-umbraco" },
        { ".gitattributes", "nf-dev-git" },
        { ".gitconfig", "nf-dev-git" },
        { ".gitignore", "nf-dev-git" },
        { ".gitmodules", "nf-dev-git" },
        { ".gitkeep", "nf-dev-git" },
        { "git-history", "nf-dev-git" },
        { "LICENSE", "nf-md-certificate" },
        { "CHANGELOG.md", "nf-fae-checklist_o" },
        { "CHANGELOG.txt", "nf-fae-checklist_o" },
        { "CHANGELOG", "nf-fae-checklist_o" },
        { "README.md", "nf-md-book_multiple" },
        { "README.txt", "nf-md-book_multiple" },
        { "README", "nf-md-book_multiple" },
        { ".DS_Store", "nf-fa-file_o" },
        { ".tsbuildinfo", "nf-seti-json" },
        { ".jscsrc", "nf-seti-json" },
        { ".jshintrc", "nf-seti-json" },
        { "tsconfig.json", "nf-seti-json" },
        { "tslint.json", "nf-seti-json" },
        { "composer.lock", "nf-seti-json" },
        { ".jsbeautifyrc", "nf-seti-json" },
        { ".esformatter", "nf-seti-json" },
        { "cdp.pid", "nf-seti-json" },
        { ".htaccess", "nf-md-xml" },
        { ".jshintignore", "nf-fa-gear" },
        { ".buildignore", "nf-fa-gear" },
        { ".mrconfig", "nf-fa-gear" },
        { ".yardopts", "nf-fa-gear" },
        { "manifest.mf", "nf-fa-gear" },
        { ".clang-format", "nf-fa-gear" },
        { ".clang-tidy", "nf-fa-gear" },
        { "favicon.ico", "nf-seti-favicon" },
        { ".travis.yml", "nf-dev-travis" },
        { ".gitlab-ci.yml", "nf-fa-gitlab" },
        { ".jenkinsfile", "nf-dev-jenkins" },
        { "bitbucket-pipelines.yml", "nf-dev-bitbucket" },
        { "bitbucket-pipelines.yaml", "nf-dev-bitbucket" },
        { ".azure-pipelines.yml", "nf-md-microsoft_azure" },

        // Firebase
        { "firebase.json", "nf-dev-firebase" },
        { ".firebaserc", "nf-dev-firebase" },

        // Bower
        { ".bowerrc", "nf-dev-bower" },
        { "bower.json", "nf-dev-bower" },

        // Conduct
        { "code_of_conduct.md", "nf-fa-handshake_o" },
        { "code_of_conduct.txt", "nf-fa-handshake_o" },

        // Docker
        { "Dockerfile", "nf-dev-docker" },
        { "docker-compose.yml", "nf-dev-docker" },
        { "docker-compose.yaml", "nf-dev-docker" },
        { "docker-compose.dev.yml", "nf-dev-docker" },
        { "docker-compose.local.yml", "nf-dev-docker" },
        { "docker-compose.ci.yml", "nf-dev-docker" },
        { "docker-compose.override.yml", "nf-dev-docker" },
        { "docker-compose.staging.yml", "nf-dev-docker" },
        { "docker-compose.prod.yml", "nf-dev-docker" },
        { "docker-compose.production.yml", "nf-dev-docker" },
        { "docker-compose.test.yml", "nf-dev-docker" },

        // Vue
        { "vue.config.js", "nf-md-vuejs" },
        { "vue.config.ts", "nf-md-vuejs" },

        // Gulp
        { "gulpfile.js", "nf-dev-gulp" },
        { "gulpfile.ts", "nf-dev-gulp" },
        { "gulpfile.babel.js", "nf-dev-gulp" },

        // Javascript
        { "gruntfile.js", "nf-seti-grunt" },

        // NodeJS
        { "package.json", "nf-dev-nodejs_small" },
        { "package-lock.json", "nf-dev-nodejs_small" },
        { ".nvmrc", "nf-dev-nodejs_small" },
        { ".esmrc", "nf-dev-nodejs_small" },

        // NPM
        { ".nmpignore", "nf-dev-npm" },
        { ".npmrc", "nf-dev-npm" },

        // Authors
        { "authors", "nf-oct-person" },
        { "authors.md", "nf-oct-person" },
        { "authors.txt", "nf-oct-person" },

        // Terraform
        { ".terraform.lock.hcl", "nf-fa-lock" },

        // Gradle
        { "gradlew", "nf-md-elephant" }
    };

    /// <summary>All nerd glyphs used by Terminal.Gui by name.</summary>
    public Dictionary<string, Rune> Glyphs { get; set; } = new ()
    {
        { "nf-cod-package", new (0xEB29) },
        { "nf-cod-preview", new (0xEB2F) },
        { "nf-custom-folder_config", new (0xE5FC) },
        { "nf-custom-folder_git", new (0xE5FB) },
        { "nf-custom-folder_git_branch", new (0xE5FB) },
        { "nf-custom-folder_github", new (0xE5FD) },
        { "nf-custom-folder_npm", new (0xE5FA) },
        { "nf-dev-aws", new (0xE7AD) },
        { "nf-dev-docker", new (0xE7B0) },
        { "nf-dev-html5", new (0xE736) },
        { "nf-fa-font", new (0xF031) },
        { "nf-fa-font_awesome", new (0xF2B4) },
        { "nf-fa-fonticons", new (0xF280) },
        { "nf-fa-github_alt", new (0xF113) },
        { "nf-fa-users", new (0xF0C0) },
        { "nf-fa-windows", new (0xF17A) },
        { "nf-md-apps", new (0xF003B) },
        { "nf-md-microsoft_azure", new (0xF0805) },
        { "nf-md-cached", new (0xF00E8) },
        { "nf-md-contacts", new (0xF06CB) },
        { "nf-md-desktop_classic", new (0xF07C0) },
        { "nf-md-folder_download", new (0xF024D) },
        { "nf-md-folder_image", new (0xF024F) },
        { "nf-md-folder_star", new (0xF069D) },
        { "nf-md-music_box_multiple", new (0xF0333) },
        { "nf-md-movie", new (0xF0381) },
        { "nf-md-movie_roll", new (0xF07DE) },
        { "nf-md-microsoft_onedrive", new (0xF03CA) },
        { "nf-md-ship_wheel", new (0xF0833) },
        { "nf-md-test_tube", new (0xF0668) },
        { "nf-md-timer", new (0xF13AB) },
        { "nf-md-timer_10", new (0xF051C) },
        { "nf-md-timer_3", new (0xF051D) },
        { "nf-md-timer_off", new (0xF13AC) },
        { "nf-md-timer_sand", new (0xF051F) },
        { "nf-md-timer_sand_empty", new (0xF06AD) },
        { "nf-md-timer_sand_full", new (0xF078C) },
        { "nf-md-umbraco", new (0xF0549) },
        { "nf-oct-file_binary", new (0xF471) },
        { "nf-oct-file_symlink_directory", new (0xF482) },
        { "nf-oct-repo", new (0xF401) },
        { "nf-oct-repo_clone", new (0xF43F) },
        { "nf-cod-repo_force_push", new (0xEB3F) },
        { "nf-oct-repo_forked", new (0xF402) },
        { "nf-oct-repo_pull", new (0xF404) },
        { "nf-oct-repo_push", new (0xF403) },
        { "nf-oct-terminal", new (0xF489) },
        { "nf-seti-config", new (0xE615) },
        { "nf-seti-project", new (0xE601) },
        { "nf-custom-elixir", new (0xE62D) },
        { "nf-custom-elm", new (0xE62C) },
        { "nf-custom-msdos", new (0xE629) },
        { "nf-custom-puppet", new (0xE631) },
        { "nf-custom-vim", new (0xE62B) },
        { "nf-dev-apple", new (0xE711) },
        { "nf-dev-bitbucket", new (0xE703) },
        { "nf-dev-bower", new (0xE74D) },
        { "nf-dev-clojure", new (0xE768) },
        { "nf-dev-clojure_alt", new (0xE76A) },
        { "nf-md-code_tags", new (0xF0174) },
        { "nf-dev-css3", new (0xE749) },
        { "nf-dev-css3_full", new (0xE74A) },
        { "nf-dev-dart", new (0xE798) },
        { "nf-dev-database", new (0xE706) },
        { "nf-dev-erlang", new (0xE7B1) },
        { "nf-dev-firebase", new (0xE787) },
        { "nf-dev-fsharp", new (0xE7A7) },
        { "nf-dev-git", new (0xE702) },
        { "nf-dev-git_branch", new (0xE725) },
        { "nf-dev-git_commit", new (0xE729) },
        { "nf-dev-git_compare", new (0xE728) },
        { "nf-dev-git_merge", new (0xE727) },
        { "nf-dev-git_pull_request", new (0xE726) },
        { "nf-dev-github", new (0xE709) },
        { "nf-cod-github_alt", new (0xEB00) },
        { "nf-dev-github_badge", new (0xE709) },
        { "nf-dev-github_full", new (0xE717) },
        { "nf-dev-go", new (0xE724) },
        { "nf-dev-googlecloud", new (0xE7F1) },
        { "nf-fa-google_drive", new (0xF2DF) },
        { "nf-dev-groovy", new (0xE775) },
        { "nf-dev-gulp", new (0xE763) },
        { "nf-dev-haskell", new (0xE777) },
        { "nf-dev-javascript", new (0xE781) },
        { "nf-dev-javascript_badge", new (0xE781) },
        { "nf-dev-javascript_alt", new (0xE74E) },
        { "nf-dev-jenkins", new (0xE767) },
        { "nf-dev-less", new (0xE758) },
        { "nf-dev-markdown", new (0xE73E) },
        { "nf-dev-nodejs_small", new (0xE718) },
        { "nf-dev-npm", new (0xE71E) },
        { "nf-dev-perl", new (0xE769) },
        { "nf-dev-php", new (0xE73D) },
        { "nf-dev-python", new (0xE73C) },
        { "nf-dev-react", new (0xE7BA) },
        { "nf-dev-rust", new (0xE7A8) },
        { "nf-dev-sass", new (0xE74B) },
        { "nf-dev-sublime", new (0xE7AA) },
        { "nf-dev-travis", new (0xE77E) },
        { "nf-dev-visualstudio", new (0xE70C) },
        { "nf-fa-archive", new (0xF187) },
        { "nf-fa-calendar", new (0xF073) },
        { "nf-fa-calendar_check_o", new (0xF274) },
        { "nf-fa-calendar_minus_o", new (0xF272) },
        { "nf-fa-calendar_o", new (0xF133) },
        { "nf-fa-calendar_plus_o", new (0xF271) },
        { "nf-fa-calendar_times_o", new (0xF273) },
        { "nf-fa-certificate", new (0xF0A3) },
        { "nf-fa-eye", new (0xF06E) },
        { "nf-fa-eye_slash", new (0xF070) },
        { "nf-fa-eyedropper", new (0xF1FB) },
        { "nf-fa-file_audio_o", new (0xF1C7) },
        { "nf-fa-file_image_o", new (0xF1C5) },
        { "nf-fa-file_o", new (0xF016) },
        { "nf-fa-file_video_o", new (0xF1C8) },
        { "nf-fa-gear", new (0xF013) },
        { "nf-fa-gears", new (0xF085) },
        { "nf-fa-gitlab", new (0xF296) },
        { "nf-fa-handshake_o", new (0xF2B5) },
        { "nf-fa-key", new (0xF084) },
        { "nf-fa-keyboard_o", new (0xF11C) },
        { "nf-fa-list", new (0xF03A) },
        { "nf-fa-list_alt", new (0xF022) },
        { "nf-fa-list_ol", new (0xF0CB) },
        { "nf-fa-list_ul", new (0xF0CA) },
        { "nf-fa-lock", new (0xF023) },
        { "nf-fae-checklist_o", new (0xE29A) },
        { "nf-fae-disco", new (0xE271) },
        { "nf-fae-java", new (0xE256) },
        { "nf-md-application", new (0xF08C6) },
        { "nf-md-certificate", new (0xF0124) },
        { "nf-md-console_line", new (0xF07B7) },
        { "nf-md-elephant", new (0xF07C6) },
        { "nf-md-file_document", new (0xF0219) },
        { "nf-md-card_text", new (0xF0B77) },
        { "nf-md-file_excel", new (0xF021B) },
        { "nf-md-file_excel_box", new (0xF021C) },
        { "nf-md-file_pdf_box", new (0xF0226) },
        { "nf-md-file_powerpoint", new (0xF0227) },
        { "nf-md-file_powerpoint_box", new (0xF0228) },
        { "nf-md-file_word", new (0xF022C) },
        { "nf-md-file_word_box", new (0xF022D) },
        { "nf-md-xml", new (0xF05C0) },
        { "nf-md-format_align_left", new (0xF0262) },
        { "nf-md-harddisk", new (0xF02CA) },
        { "nf-md-help_box", new (0xF078B) },
        { "nf-md-language_c", new (0xF0671) },
        { "nf-md-language_cpp", new (0xF0672) },
        { "nf-md-language_csharp", new (0xF031B) },
        { "nf-md-language_css3", new (0xF031C) },
        { "nf-md-language_r", new (0xF07D4) },
        { "nf-md-book_multiple", new (0xF00BB) },
        { "nf-md-notebook", new (0xF082E) },
        { "nf-md-package_variant", new (0xF03D6) },
        { "nf-md-package_variant_closed", new (0xF03D7) },
        { "nf-md-svg", new (0xF0721) },
        { "nf-md-microsoft_visual_studio", new (0xF0610) },
        { "nf-md-vuejs", new (0xF0844) },
        { "nf-md-language_xaml", new (0xF0673) },
        { "nf-oct-file_zip", new (0xF410) },
        { "nf-oct-person", new (0xF415) },
        { "nf-oct-ruby", new (0xF43B) },
        { "nf-seti-favicon", new (0xE623) },
        { "nf-seti-grunt", new (0xE611) },
        { "nf-seti-html", new (0xE60E) },
        { "nf-seti-json", new (0xE60B) },
        { "nf-seti-julia", new (0xE624) },
        { "nf-seti-lua", new (0xE620) },
        { "nf-seti-mustache", new (0xE60F) },
        { "nf-seti-typescript", new (0xE628) },
    };

    public Rune GetNerdIcon (IFileSystemInfo? file, bool isOpen)
    {
        ArgumentNullException.ThrowIfNull (file);

        string filename = file.Name;

        if (FilenameToIcon.TryGetValue (filename, out string? filenameIcon))
        {
            return Glyphs [filenameIcon];
        }

        for (int suffixStart = filename.IndexOf ('.'); suffixStart >= 0; suffixStart = filename.IndexOf ('.', suffixStart + 1))
        {
            if (!ExtensionToIcon.TryGetValue (filename [suffixStart..], out string? extensionIcon))
            {
                continue;
            }

            return Glyphs [extensionIcon];
        }

        if (file is IDirectoryInfo)
        {
            return isOpen ? _nf_cod_folder_opened : _nf_cod_folder;
        }

        return _nf_cod_file;
    }
}
