# 📦 PromptVault

**A modern, open-source Windows application for managing AI prompts with lightning-fast access.**

![Version](https://img.shields.io/badge/version-1.0.0-blue)
![License](https://img.shields.io/badge/license-MIT-green)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)

## ✨ Features

### 🚀 Core Features
- **Lightning Fast Access** - Open instantly with customizable hotkeys from anywhere
- **Smart Search** - Real-time search across titles, content, tags, and metadata
- **Custom Hotkeys** - Configure global shortcuts for "Open App" and "Quick Capture"
- **System Tray** - Run in background with minimize-to-tray support
- **Offline-First** - All data stored locally in SQLite, no internet required
- **Modern UI** - Clean, responsive interface with smooth dark/light theme switching

### 📊 Organization & Management
- **Smart Filtering** - Multi-filter by AI platform, model version, and tags
- **Favorites** - Star important prompts for quick access
- **Tag Management** - Organize with unlimited custom tags and colors
- **Usage Tracking** - See which prompts you use most often
- **Quick Actions** - Copy, edit, or delete with one click

### 📥 Import & Export
- **Bulk Import** - Import from CSV files or multiple text files
- **Import Wizard** - Step-by-step guidance with preview and validation
- **Export Options** - Export all prompts to CSV or individual text files
- **Backup & Restore** - One-click database backup and restore

### ⌨️ Keyboard & Accessibility
- **Customizable Hotkeys**:
  - Open Application (default: `Ctrl+Shift+V`)
  - Quick Clipboard Capture (default: `Ctrl+Shift+C`)
- **Placeholder Search** - Intuitive search with focus management
- **Quick Clipboard** - Save clipboard content as prompt instantly

### 🎨 Appearance
- **Dynamic Themes** - Switch between dark and light modes instantly
- **Persistent Settings** - Your preferences saved automatically
- **Modern Card Design** - Beautiful, hover-enabled prompt cards
- **Color-Coded Tags** - Visual organization with automatic tag colors
- **Model Badges** - Instantly see which AI platform each prompt is for

## 🎯 Use Cases

- **Developers**: Code review, debugging, documentation, and testing prompts
- **Writers**: Content creation, editing, SEO optimization, and blog post templates
- **Marketers**: Social media, ad copy, email campaigns, and brand messaging
- **Analysts**: Data analysis, reporting, visualization, and insight generation
- **Researchers**: Literature reviews, summarization, and research assistance
- **Everyone**: Daily AI interactions, learning prompts, and productivity boosters

## 🚀 Quick Start

### Prerequisites

- Windows 10/11 (64-bit)
- .NET 8.0 Runtime (automatically installed with app)

### Installation

#### Option 1: Download Release (Recommended)
1. Go to [Releases](https://github.com/ritesh-sri/PromptVault/releases)
2. Download the latest `PromptVault-Setup.exe`
3. Run the installer
4. Launch PromptVault from Start Menu or Desktop
5. Press `Ctrl+Shift+V` from anywhere to open!

#### Option 2: Build from Source

```bash
# Clone the repository
git clone https://github.com/ritesh-sri/promptvault.git
cd promptvault

# Restore dependencies
dotnet restore

# Build the project
dotnet build --configuration Release

# Run the application
dotnet run
```

## 📖 Usage Guide

### Getting Started

1. **First Launch**: App opens with sample prompts to get you started
2. **Add Prompts**: Click "➕ New Prompt" or press `Ctrl+Shift+C` to capture from clipboard
3. **Organize**: Add tags, set AI platform and model version
4. **Quick Access**: Press `Ctrl+Shift+V` anytime to open from anywhere

### Global Hotkeys

Configure in **Settings > Hotkeys**:

- **Open Application**: `Ctrl+Shift+V` (customizable)
  - Opens PromptVault instantly from any application
  - Restores from system tray if minimized
  
- **Quick Capture**: `Ctrl+Shift+C` (customizable)
  - Opens app with clipboard content pre-filled
  - Ready to save immediately

**How to Change Hotkeys:**
1. Open Settings (⚙️ button)
2. Navigate to "Hotkeys" tab
3. Click "✏️ Change" button
4. Press your desired key combination
5. Click "Set Hotkey"

### System Tray

**Enable:** Settings > General > "Minimize to System Tray"

**Features:**
- App stays running when window is closed
- Double-click tray icon to restore
- Right-click for quick actions menu
- Balloon notifications for important events

### Adding Prompts

**Method 1: New Prompt**
- Click "➕ New Prompt" button
- Fill in title, content, AI platform, model, and tags
- Mark as favorite if needed
- Click "Save"

**Method 2: Quick Clipboard Capture**
1. Copy any text to clipboard
2. Press `Ctrl+Shift+C` (or your custom hotkey)
3. PromptVault opens with content pre-filled
4. Add title and metadata
5. Save

**Method 3: Bulk Import**
1. Click "📥 Import" button
2. Choose CSV file or multiple text files
3. Review preview
4. Configure import options
5. Import

### Managing Prompts

- **Copy to Clipboard**: Click "📋 Copy" button
- **Edit**: Click "✏️" icon
- **Delete**: Click "🗑️" icon (with confirmation)
- **Favorite**: Click "⭐" to toggle favorite status
- **Track Usage**: Copy count automatically increments

### Filtering & Search

**Search Bar:**
- Type to search across titles, content, tags, AI providers, and models
- Real-time filtering as you type
- Case-insensitive matching

**Sidebar Filters:**
- **AI Platform**: ChatGPT, Claude, Gemini, Copilot, etc.
- **Model Version**: GPT-4, Claude 3.5, Gemini Pro, etc.
- **Tags**: Your custom tags
- **Favorites Only**: Show only starred prompts

**Combine Filters:**
- Use search + filters together
- Multiple filters apply simultaneously
- Clear filters anytime

### Themes

**Toggle Modes:**
- Click 🌙/☀️ button in top-right
- Or change in Settings > General > Appearance Theme

**Theme Options:**
- ☀️ Light Theme
- 🌙 Dark Theme
- 💻 System Default (coming soon)

**Automatic Persistence:**
- Your theme choice is remembered
- Applies across all windows and dialogs

### Settings & Tools

**General Settings:**
- Appearance theme
- Launch at startup
- Minimize to system tray
- Auto-backup prompts (coming soon)

**Hotkeys:**
- Configure global shortcuts
- Real-time validation
- Conflict detection

**Data Management:**
- Database location and info
- Backup database
- Restore from backup
- Export all prompts (CSV or TXT)
- Clear all data (with warnings)

**About:**
- Version information
- Check for updates
- Documentation links
- Report bugs

## 🏗️ Project Structure

```
PromptVault/
├── Models/
│   └── Prompt.cs              # Data models and constants
├── Services/
│   ├── DatabaseService.cs     # SQLite operations
│   ├── ImportService.cs       # CSV/Text import/export
│   ├── HotkeyManager.cs       # Global hotkey handling
│   ├── ThemeManager.cs        # Theme state & persistence
│   └── SystemTrayManager.cs   # System tray integration
├── Dialogs/
│   ├── AddEditPromptDialog.xaml      # Create/Edit prompts
│   ├── ImportWizardDialog.xaml       # Import wizard
│   ├── SettingsDialog.xaml           # Settings & tools
│   └── HotkeyInputDialog.xaml        # Hotkey configuration
├── MainWindow.xaml            # Main UI
├── App.xaml                   # Application resources
└── PromptVault.csproj         # Project configuration
```

## 🗄️ Database

**Location:** `%AppData%\PromptVault\prompts.db`

**Schema:**

**Prompts Table:**
```sql
- Id (INTEGER PRIMARY KEY)
- Title (TEXT NOT NULL)
- Content (TEXT NOT NULL)
- AIProvider (TEXT)
- ModelVersion (TEXT)
- IsFavorite (BOOLEAN)
- CreatedAt (DATETIME)
- UpdatedAt (DATETIME)
- UsageCount (INTEGER)
```

**Tags Table:**
```sql
- Id (INTEGER PRIMARY KEY)
- Name (TEXT UNIQUE)
- Color (TEXT)
```

**PromptTags Table:**
```sql
- PromptId (INTEGER FOREIGN KEY)
- TagId (INTEGER FOREIGN KEY)
```

## 🛠️ Configuration

### Settings File

**Location:** `%AppData%\PromptVault\settings.txt`

**Format:**
```
Theme=Dark
MinimizeToTray=True
OpenHotkey=Ctrl + Shift + V
ClipboardHotkey=Ctrl + Shift + C
```

### Customization

All settings accessible through Settings dialog:
- Theme preference
- System tray behavior
- Custom hotkeys
- Startup options

## 🗺️ Roadmap

### ✅ v1.0.0 (Current - Released)
- ✅ Prompt CRUD operations
- ✅ SQLite database
- ✅ Search & filtering
- ✅ Import/Export (CSV & TXT)
- ✅ Custom hotkeys
- ✅ System tray integration
- ✅ Dark/Light themes
- ✅ Tag management
- ✅ Favorites system
- ✅ Usage tracking

### 🚧 v1.2.0 (In Progress)
- [ ] **Token Counter** - Real-time token estimation with cost calculation
- [ ] **Prompt Templates** - Variables like {{topic}} for reusable prompts
- [ ] **Keyboard Shortcuts** - Ctrl+N, Ctrl+F, etc. for power users
- [ ] **Statistics Dashboard** - Usage analytics and insights
- [ ] **Export Formats** - Markdown, JSON export options
- [ ] **Multi-Select** - Bulk operations on multiple prompts

### 📅 v0.3.0 (Planned)
- [ ] **Collections/Folders** - Hierarchical organization
- [ ] **Prompt History** - Track edits and restore previous versions
- [ ] **Smart Auto-Tagging** - AI-powered tag suggestions
- [ ] **Command Palette** - Quick action search (Ctrl+K)
- [ ] **Rich Text Preview** - Markdown rendering
- [ ] **Drag & Drop** - File import and reordering

### 🔮 v1.0.0 (Future)
- [ ] **Prompt Chaining** - Multi-step prompt workflows
- [ ] **Cloud Sync** - Optional OneDrive/Google Drive sync
- [ ] **Plugin System** - Extend functionality
- [ ] **Community Marketplace** - Share and discover prompts
- [ ] **Mobile Companion** - Android/iOS apps

## 🤝 Contributing

Contributions are welcome! Here's how:

### Ways to Contribute

1. **Report Bugs** - Open an issue with details
2. **Suggest Features** - Share your ideas in discussions
3. **Submit PRs** - Fix bugs or add features
4. **Improve Docs** - Help others understand the project
5. **Share** - Star the repo and tell others!

### Development Setup

```bash
# Fork and clone
git clone https://github.com/ritesh-sri/promptvault.git
cd promptvault

# Create feature branch
git checkout -b feature/amazing-feature

# Make changes and commit
git commit -m "Add amazing feature"

# Push and create PR
git push origin feature/amazing-feature
```

### Code Guidelines

- Follow C# naming conventions
- Add XML comments for public methods
- Test thoroughly before submitting
- Keep PRs focused and small
- Update README if adding features

## 🐛 Bug Reports

**Found a bug?** Open an issue with:

- **Description**: What happened?
- **Steps to Reproduce**: How to trigger it?
- **Expected Behavior**: What should happen?
- **Screenshots**: Visual proof helps!
- **Environment**:
  - Windows version
  - PromptVault version
  - .NET version

## 📄 License

MIT License - see [LICENSE](LICENSE) file

**TLDR:** Free to use, modify, and distribute. No warranty.

## 🙏 Acknowledgments

- **Built with**: WPF (.NET 8.0)
- **Database**: SQLite
- **CSV Parsing**: CsvHelper
- **Icons**: Unicode emojis
- **Inspired by**: The amazing AI community

## 📞 Support & Contact

- **Issues**: [GitHub Issues](https://github.com/ritesh-sri/promptvault/issues)
- **Discussions**: [GitHub Discussions](https://github.com/ritesh-sri/promptvault/discussions)
- **Email**: [Your email if you want]

## ⚡ Performance

- **Startup**: <500ms cold start
- **Memory**: ~50-100MB RAM usage
- **Database**: Handles 10,000+ prompts smoothly
- **Hotkey Response**: <50ms
- **Search**: Real-time, <100ms for 1000+ prompts

## 🔒 Privacy & Security

- **100% Offline** - No cloud, no tracking
- **Local Storage** - Your data stays on your machine
- **No Telemetry** - We don't know you exist
- **Open Source** - Audit the code anytime
- **Encrypted Backups** - Optional encryption (coming soon)

## 💡 Tips & Tricks

1. **Double-Click to Copy** - Fastest way to grab a prompt (coming soon)
2. **Use Tags Wisely** - Create categories like "work", "personal", "urgent"
3. **Favorite Frequently Used** - Quick access via Favorites filter
4. **Descriptive Titles** - Makes searching easier
5. **Regular Backups** - Settings > Data > Backup Database
6. **Hotkey Mastery** - Customize to your workflow
7. **Template Variables** - Use {{placeholder}} for reusable prompts (v0.2.0)

## 📸 Screenshots

### Light Theme
![Light Theme](Light_theme.png)
*Clean, modern interface for daytime use*

### Dark Theme
![Dark Theme](Dark_theme.png)
*Easy on the eyes for night coding sessions*

### Add Prompt Dialog
![Add Prompt](add_new_prompt.png)
*Intuitive form with all the metadata you need*

### Settings Window
![Settings](setting.png)
*Comprehensive configuration options*

## 🎓 Tutorials

### Getting Started (5 minutes)
1. Install PromptVault
2. Press `Ctrl+Shift+V` to open
3. Click "➕ New Prompt"
4. Fill in your first prompt
5. Start organizing with tags!

### Importing Existing Prompts
1. Prepare CSV with columns: Title, Content, AIProvider, ModelVersion, Tags
2. Click "📥 Import"
3. Select your CSV
4. Review preview
5. Click "Import"

### Power User Workflow
1. Set up custom hotkeys
2. Enable minimize to tray
3. Use Quick Capture for instant saves
4. Organize with tags and favorites
5. Export backups regularly

## ❓ FAQ

**Q: Does this work offline?**  
A: Yes! 100% offline. No internet required.

**Q: Where is my data stored?**  
A: `%AppData%\PromptVault\prompts.db` - Fully under your control.

**Q: Can I sync across devices?**  
A: Not yet, but planned for v1.0.0 with optional cloud sync.

**Q: Is my data secure?**  
A: It never leaves your machine. Open source = audit the code yourself.

**Q: Can I import from [other app]?**  
A: Currently supports CSV and TXT. Export from your app to these formats.

**Q: How do I reset everything?**  
A: Settings > Data > Clear All Data (creates backup first).

---

**Made with ❤️ for the AI community**

[⬆️ Back to top](#-promptvault)