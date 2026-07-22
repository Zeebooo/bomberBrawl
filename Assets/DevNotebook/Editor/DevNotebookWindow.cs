using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DevNotebook.Editor
{
    internal sealed class DevNotebookWindow : EditorWindow
    {
        private static readonly List<string> StatusChoices = new List<string>
        {
            "All",
            "None",
            "Todo",
            "In Progress",
            "Done",
            "Blocked"
        };

        private string _pendingNotebookGuid;
        private int _pendingSectionIndex = -1;
        private int _pendingPageIndex = -1;

        private string _currentNotebookGuid;
        private DevNotebookNotebook _currentNotebook;
        private List<string> _notebookGuids = new List<string>();
        private int _selectedSectionIndex = -1;
        private int _selectedPageIndex = -1;
        private string _searchText = string.Empty;
        private int _statusFilterIndex;
        private bool _currentSceneOnly;
        private bool _isNotebookEditMode;
        private bool _isPageEditMode;
        private int _editingSectionIndex = -1;
        private int _editingPageIndex = -1;
        private int _lastSectionClickIndex = -1;
        private double _lastSectionClickTime;
        private int _lastPageClickIndex = -1;
        private double _lastPageClickTime;

        private DropdownField _notebookDropdown;
        private Label _windowTitleLabel;
        private Label _windowSubtitleLabel;
        private Button _modeToggleButton;
        private VisualElement _notebookDetailsHost;
        private ScrollView _sectionListScroll;
        private ScrollView _pageListScroll;
        private VisualElement _pageEditorHost;
        private TextField _searchField;
        private DropdownField _statusDropdown;
        private Toggle _currentSceneToggle;
        private Button _createSectionButton;
        private Button _deleteSectionButton;
        private Button _createPageButton;
        private Button _editPageButton;
        private Button _deletePageButton;

        public static DevNotebookWindow ShowWindow(string notebookGuid = "", int sectionIndex = -1, int pageIndex = -1)
        {
            DevNotebookWindow window = GetWindow<DevNotebookWindow>();
            window.titleContent = new GUIContent("DevNotebook");
            window.minSize = new Vector2(1160f, 720f);
            window.SetPendingSelection(notebookGuid, sectionIndex, pageIndex);
            window.RebuildIfReady();
            return window;
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("DevNotebook");
            minSize = new Vector2(1160f, 720f);
        }

        public void CreateGUI()
        {
            BuildUi();
            ApplyPendingSelection();
            RefreshAll();
        }

        private void SetPendingSelection(string notebookGuid, int sectionIndex, int pageIndex)
        {
            _pendingNotebookGuid = notebookGuid ?? string.Empty;
            _pendingSectionIndex = sectionIndex;
            _pendingPageIndex = pageIndex;
        }

        private void RebuildIfReady()
        {
            if (rootVisualElement.childCount == 0)
            {
                return;
            }

            ApplyPendingSelection();
            RefreshAll();
        }

        private void ApplyPendingSelection()
        {
            if (string.IsNullOrWhiteSpace(_pendingNotebookGuid))
            {
                return;
            }

            _currentNotebookGuid = _pendingNotebookGuid;
            _selectedSectionIndex = _pendingSectionIndex;
            _selectedPageIndex = _pendingPageIndex;
            _pendingNotebookGuid = string.Empty;
            _pendingSectionIndex = -1;
            _pendingPageIndex = -1;
        }

        private void BuildUi()
        {
            rootVisualElement.Clear();

            StyleSheet styleSheet = DevNotebookEditorPaths.LoadStyleSheet();
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            rootVisualElement.AddToClassList("dn-root");
            rootVisualElement.AddToClassList(EditorGUIUtility.isProSkin ? "theme-dark" : "theme-light");

            var shell = new VisualElement();
            shell.AddToClassList("dn-shell");
            rootVisualElement.Add(shell);

            shell.Add(BuildHeader());
            shell.Add(BuildContent());
        }

        private VisualElement BuildHeader()
        {
            var header = new VisualElement();
            header.AddToClassList("dn-header");

            var eyebrow = new Label("DevNotebook");
            eyebrow.AddToClassList("dn-eyebrow");
            header.Add(eyebrow);

            _windowTitleLabel = new Label("Keep your project notes attached to the work");
            _windowTitleLabel.AddToClassList("dn-hero-title");
            header.Add(_windowTitleLabel);

            _windowSubtitleLabel = new Label("Create notebooks, track review work, and jump back to linked scenes and assets without leaving the editor.");
            _windowSubtitleLabel.AddToClassList("dn-hero-copy");
            header.Add(_windowSubtitleLabel);

            var toolbarRow = new VisualElement();
            toolbarRow.AddToClassList("dn-toolbar-row");
            header.Add(toolbarRow);

            _notebookDropdown = new DropdownField("Notebook", new List<string> { "No notebooks yet" }, 0);
            _notebookDropdown.AddToClassList("dn-dropdown-wide");
            _notebookDropdown.RegisterValueChangedCallback(OnNotebookDropdownChanged);
            toolbarRow.Add(_notebookDropdown);

            Button createNotebookButton = CreateButton("Create Notebook", "dn-button-primary", () =>
            {
                DevNotebookNotebook notebook = DevNotebookCreationUtility.CreateNotebookInSelectedFolder(true);
                if (notebook == null)
                {
                    return;
                }

                string guid = DevNotebookNotebookRepository.GetGuid(notebook);
                _currentNotebookGuid = guid;
                _selectedSectionIndex = -1;
                _selectedPageIndex = -1;
                RefreshAll();
            });
            toolbarRow.Add(createNotebookButton);

            toolbarRow.Add(CreateButton("Getting Started", "dn-button-subtle", () => DevNotebookGettingStartedWindow.ShowWindow()));
            toolbarRow.Add(CreateButton("Project Settings", "dn-button-subtle", () => SettingsService.OpenProjectSettings(DevNotebookEditorPaths.SettingsPath)));
            _modeToggleButton = CreateButton("Edit Notebook", "dn-button-primary", ToggleNotebookEditMode);
            toolbarRow.Add(_modeToggleButton);
            header.Add(BuildNotebookDetailsCard());

            return header;
        }

        private VisualElement BuildContent()
        {
            var content = new VisualElement();
            content.AddToClassList("dn-content");

            var outerSplit = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal);
            outerSplit.AddToClassList("dn-split-view");
            outerSplit.AddToClassList("dn-split-outer");

            VisualElement leftPane = BuildLeftPane();
            leftPane.style.minWidth = 260f;
            outerSplit.Add(leftPane);

            var rightSplit = new TwoPaneSplitView(0, 360, TwoPaneSplitViewOrientation.Horizontal);
            rightSplit.AddToClassList("dn-split-view");
            rightSplit.AddToClassList("dn-split-inner");
            rightSplit.style.flexGrow = 1f;

            VisualElement middlePane = BuildMiddlePane();
            middlePane.style.minWidth = 320f;
            rightSplit.Add(middlePane);

            VisualElement rightPane = BuildRightPane();
            rightPane.style.minWidth = 420f;
            rightSplit.Add(rightPane);

            outerSplit.Add(rightSplit);
            content.Add(outerSplit);

            return content;
        }

        private VisualElement BuildLeftPane()
        {
            var pane = new VisualElement();
            pane.AddToClassList("dn-pane");
            pane.AddToClassList("dn-pane-left");

            pane.Add(CreatePaneHeader("Sections", "Group related notes and review passes"));

            var sectionActions = new VisualElement();
            sectionActions.AddToClassList("dn-inline-actions");
            _createSectionButton = CreateButton("New Section", "dn-button-primary", CreateSection);
            _deleteSectionButton = CreateButton("Delete Section", "dn-button-danger", DeleteSelectedSection);
            sectionActions.Add(_createSectionButton);
            sectionActions.Add(_deleteSectionButton);
            pane.Add(sectionActions);

            _sectionListScroll = new ScrollView();
            _sectionListScroll.AddToClassList("dn-list-scroll");
            _sectionListScroll.AddManipulator(new ContextualMenuManipulator(BuildSectionListContextMenu));
            pane.Add(_sectionListScroll);

            return pane;
        }

        private VisualElement BuildNotebookDetailsCard()
        {
            var card = new VisualElement();
            card.AddToClassList("dn-header-details-shell");
            _notebookDetailsHost = new VisualElement();
            card.Add(_notebookDetailsHost);

            return card;
        }

        private VisualElement BuildMiddlePane()
        {
            var pane = new VisualElement();
            pane.AddToClassList("dn-pane");
            pane.AddToClassList("dn-pane-middle");

            pane.Add(CreatePaneHeader("Pages", "Searchable notes, tags, and scene-aware review"));

            var filterCard = CreateCard();

            _searchField = new TextField("Search")
            {
                isDelayed = true
            };
            _searchField.RegisterValueChangedCallback(evt =>
            {
                _searchText = evt.newValue ?? string.Empty;
                RefreshPages();
            });
            filterCard.Add(_searchField);

            _statusDropdown = new DropdownField("Status", StatusChoices, 0);
            _statusDropdown.RegisterValueChangedCallback(evt =>
            {
                _statusFilterIndex = Math.Max(0, StatusChoices.IndexOf(evt.newValue));
                RefreshPages();
            });
            filterCard.Add(_statusDropdown);

            _currentSceneToggle = new Toggle("Current Scene Only");
            _currentSceneToggle.RegisterValueChangedCallback(evt =>
            {
                _currentSceneOnly = evt.newValue;
                RefreshPages();
            });
            filterCard.Add(_currentSceneToggle);

            pane.Add(filterCard);

            var pageActions = new VisualElement();
            pageActions.AddToClassList("dn-inline-actions");
            _createPageButton = CreateButton("New Page", "dn-button-primary", CreatePage);
            _editPageButton = CreateButton("Edit Page", "dn-button-subtle", ToggleSelectedPageMode);
            _deletePageButton = CreateButton("Delete Page", "dn-button-danger", DeleteSelectedPage);
            pageActions.Add(_createPageButton);
            pageActions.Add(_editPageButton);
            pageActions.Add(_deletePageButton);
            pane.Add(pageActions);

            _pageListScroll = new ScrollView();
            _pageListScroll.AddToClassList("dn-list-scroll");
            _pageListScroll.AddManipulator(new ContextualMenuManipulator(BuildPageListContextMenu));
            pane.Add(_pageListScroll);

            return pane;
        }

        private VisualElement BuildRightPane()
        {
            var pane = new VisualElement();
            pane.AddToClassList("dn-pane");
            pane.AddToClassList("dn-pane-right");

            pane.Add(CreatePaneHeader("Page Editor", "Write notes, track progress, and jump back into context"));

            _pageEditorHost = new VisualElement();
            _pageEditorHost.AddToClassList("dn-editor-host");
            pane.Add(_pageEditorHost);

            return pane;
        }

        private VisualElement CreatePaneHeader(string titleText, string subtitleText)
        {
            var header = new VisualElement();
            header.AddToClassList("dn-pane-header");

            var title = new Label(titleText);
            title.AddToClassList("dn-pane-title");
            header.Add(title);

            var subtitle = new Label(subtitleText);
            subtitle.AddToClassList("dn-pane-subtitle");
            header.Add(subtitle);

            return header;
        }

        private VisualElement CreateCard()
        {
            var card = new VisualElement();
            card.AddToClassList("dn-card");
            return card;
        }

        private Button CreateButton(string text, string className, Action onClick)
        {
            Button button = new Button(() => onClick?.Invoke())
            {
                text = text
            };
            button.AddToClassList(className);
            return button;
        }

        private void RefreshAll()
        {
            ReloadNotebookOptions();
            EnsureNotebookLoaded();
            RefreshHeader();
            RefreshNotebookDetails();
            RefreshSections();
            RefreshPages();
            RefreshEditor();
            RefreshActionState();
        }

        private void ReloadNotebookOptions()
        {
            _notebookGuids = DevNotebookNotebookRepository.FindNotebookGuids();
            List<string> notebookNames = _notebookGuids.Count == 0
                ? new List<string> { "No notebooks yet" }
                : _notebookGuids.Select(DevNotebookNotebookRepository.GetDisplayName).ToList();

            _notebookDropdown.choices = notebookNames;

            int index = -1;
            if (!string.IsNullOrWhiteSpace(_currentNotebookGuid))
            {
                index = _notebookGuids.IndexOf(_currentNotebookGuid);
            }

            if (index < 0 && _notebookGuids.Count > 0)
            {
                index = 0;
                _currentNotebookGuid = _notebookGuids[0];
            }

            if (_notebookGuids.Count == 0)
            {
                _currentNotebookGuid = string.Empty;
                _notebookDropdown.SetValueWithoutNotify("No notebooks yet");
                return;
            }

            _notebookDropdown.SetValueWithoutNotify(notebookNames[Mathf.Clamp(index, 0, notebookNames.Count - 1)]);
        }

        private void EnsureNotebookLoaded()
        {
            _currentNotebook = string.IsNullOrWhiteSpace(_currentNotebookGuid)
                ? null
                : DevNotebookNotebookRepository.LoadNotebook(_currentNotebookGuid);

            if (_currentNotebook == null)
            {
                _selectedSectionIndex = -1;
                _selectedPageIndex = -1;
                return;
            }

            _currentNotebook.EnsureData();

            if (_selectedSectionIndex < 0 && _currentNotebook.sections.Count > 0)
            {
                _selectedSectionIndex = 0;
            }

            _selectedSectionIndex = Mathf.Clamp(_selectedSectionIndex, 0, Mathf.Max(_currentNotebook.sections.Count - 1, 0));

            if (_currentNotebook.sections.Count == 0)
            {
                _selectedSectionIndex = -1;
                _selectedPageIndex = -1;
            }
            else
            {
                DevNotebookSection section = _currentNotebook.sections[_selectedSectionIndex];
                if (section.pages.Count == 0)
                {
                    _selectedPageIndex = -1;
                }
                else if (_selectedPageIndex < 0 || _selectedPageIndex >= section.pages.Count)
                {
                    _selectedPageIndex = 0;
                }
            }

            DevNotebookProjectSettings.instance.RecordNotebookOpened(_currentNotebookGuid, _currentNotebook.title);
        }

        private void RefreshHeader()
        {
            if (_currentNotebook == null)
            {
                _windowTitleLabel.text = "Keep your project notes attached to the work";
                _windowSubtitleLabel.text = "Create notebooks, track review work, and jump back to linked scenes and assets without leaving the editor.";
                return;
            }

            _windowTitleLabel.text = string.IsNullOrWhiteSpace(_currentNotebook.title)
                ? "DevNotebook Workspace"
                : _currentNotebook.title;

            _windowSubtitleLabel.text = string.IsNullOrWhiteSpace(_currentNotebook.description)
                ? "Use sections and pages to keep review notes close to your project content."
                : _currentNotebook.description;
        }

        private void RefreshNotebookDetails()
        {
            _notebookDetailsHost.Clear();

            if (_currentNotebook == null)
            {
                _notebookDetailsHost.Add(CreateInlineHint("Create or open a notebook to see its details here."));
                return;
            }

            if (!_isNotebookEditMode)
            {
                var summaryRow = new VisualElement();
                summaryRow.AddToClassList("dn-header-details-row");

                summaryRow.Add(CreateNotebookMetric("Notebook", string.IsNullOrWhiteSpace(_currentNotebook.title) ? "Untitled Notebook" : _currentNotebook.title));
                summaryRow.Add(CreateNotebookMetric("Sections", _currentNotebook.sections.Count.ToString()));
                summaryRow.Add(CreateNotebookMetric("Pages", GetTotalPageCount().ToString()));

                summaryRow.Add(CreateNotebookMetric("Theme", "Static palette"));
                _notebookDetailsHost.Add(summaryRow);
                return;
            }

            var editRow = new VisualElement();
            editRow.AddToClassList("dn-header-edit-row");

            var titleField = new TextField("Title")
            {
                value = _currentNotebook.title,
                isDelayed = true
            };
            titleField.RegisterValueChangedCallback(evt =>
            {
                MutateNotebook("Rename Notebook", () =>
                {
                    _currentNotebook.title = evt.newValue;
                });
            });
            titleField.AddToClassList("dn-header-field-grow");
            editRow.Add(titleField);

            var descriptionField = new TextField("Description")
            {
                value = _currentNotebook.description,
                isDelayed = true
            };
            descriptionField.AddToClassList("dn-header-field-grow");
            descriptionField.RegisterValueChangedCallback(evt =>
            {
                MutateNotebook("Edit Notebook Description", () =>
                {
                    _currentNotebook.description = evt.newValue;
                }, refreshSections: false, refreshPages: false, refreshEditor: false, refreshNotebookDetails: false, refreshHeader: true);
            });
            editRow.Add(descriptionField);

            editRow.Add(CreateInlineHint("Theme accents are fixed in V1 for readability."));

            _notebookDetailsHost.Add(editRow);
        }

        private void RefreshSections()
        {
            _sectionListScroll.Clear();

            if (_currentNotebook == null)
            {
                _sectionListScroll.Add(CreateEmptyState("No notebook loaded yet.", "Create a notebook to start organizing notes."));
                return;
            }

            if (_currentNotebook.sections.Count == 0)
            {
                _sectionListScroll.Add(CreateEmptyState("No sections yet.", "Create your first section to start grouping notes."));
                return;
            }

            foreach (SectionReference reference in GetOrderedSections())
            {
                int localIndex = reference.SectionIndex;
                DevNotebookSection section = reference.Section;
                VisualElement card = CreateListCard(localIndex == _selectedSectionIndex, () => HandleSectionCardClick(localIndex));
                card.AddToClassList("dn-section-card");

                var row = new VisualElement();
                row.AddToClassList("dn-list-row");

                var titleColumn = new VisualElement();
                titleColumn.AddToClassList("dn-list-copy");

                var title = new Label(string.IsNullOrWhiteSpace(section.title) ? "Untitled Section" : section.title);
                title.AddToClassList("dn-list-title");
                titleColumn.Add(title);

                var subtitle = new Label(section.pages.Count == 1 ? "1 page" : $"{section.pages.Count} pages");
                subtitle.AddToClassList("dn-list-subtitle");
                titleColumn.Add(subtitle);

                row.Add(titleColumn);

                if (section.isPinned)
                {
                    row.Add(CreatePinMarker());
                }

                card.Add(row);
                card.Add(CreateSectionVisualization(section));

                if (localIndex == _selectedSectionIndex && _editingSectionIndex == localIndex)
                {
                    card.Add(BuildSelectedSectionEditor(section));
                }

                AttachSectionCardContextMenu(card, localIndex);
                _sectionListScroll.Add(card);
            }
        }

        private VisualElement BuildSelectedSectionEditor(DevNotebookSection section)
        {
            var container = new VisualElement();
            container.AddToClassList("dn-section-editor");
            container.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
            container.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

            var titleField = new TextField("Section Title")
            {
                value = section.title,
                isDelayed = true
            };
            titleField.RegisterValueChangedCallback(evt =>
            {
                MutateNotebook("Rename Section", () =>
                {
                    section.title = evt.newValue;
                }, refreshPages: false, refreshEditor: false, refreshNotebookDetails: false, refreshHeader: false);
            });
            container.Add(titleField);

            container.Add(CreateButton("Done", "dn-button-subtle", () =>
            {
                _editingSectionIndex = -1;
                RefreshSections();
            }));

            return container;
        }

        private void RefreshPages()
        {
            _pageListScroll.Clear();

            if (_currentNotebook == null)
            {
                _pageListScroll.Add(CreateEmptyState("No notebook open.", "Select or create a notebook to work with pages."));
                return;
            }

            if (_selectedSectionIndex < 0 || _selectedSectionIndex >= _currentNotebook.sections.Count)
            {
                _pageListScroll.Add(CreateEmptyState("No section selected.", "Pick a section or create one to start adding pages."));
                return;
            }

            List<PageReference> filteredPages = GetFilteredPages();
            if (filteredPages.Count == 0)
            {
                _pageListScroll.Add(CreateEmptyState("No pages match the current filters.", "Try a different filter or create a new page."));
                return;
            }

            foreach (PageReference reference in filteredPages)
            {
                VisualElement card = CreateListCard(reference.PageIndex == _selectedPageIndex, () => HandlePageCardClick(reference.PageIndex));
                card.AddToClassList("dn-page-list-card");
                bool isSelected = reference.PageIndex == _selectedPageIndex;
                VisualElement quickActions = CreatePageQuickActions(reference.PageIndex, reference.Page);
                quickActions.style.display = isSelected ? DisplayStyle.Flex : DisplayStyle.None;

                card.Add(CreatePageCardContents(reference.Page, reference.PageIndex));
                card.Add(quickActions);
                AttachPageCardContextMenu(card, reference.PageIndex);
                _pageListScroll.Add(card);
            }
        }

        private VisualElement CreatePageCardContents(DevNotebookPage page, int pageIndex)
        {
            var container = new VisualElement();
            container.AddToClassList("dn-page-card");

            var topRow = new VisualElement();
            topRow.AddToClassList("dn-card-top-row");

            var title = new Label(string.IsNullOrWhiteSpace(page.title) ? "Untitled Page" : page.title);
            title.AddToClassList("dn-list-title");
            topRow.Add(title);

            if (page.isPinned)
            {
                topRow.Add(CreatePinMarker());
            }

            var statusChip = new Label(GetStatusLabel(page.status));
            statusChip.AddToClassList("dn-status-chip");
            statusChip.AddToClassList(GetStatusClass(page.status));
            topRow.Add(statusChip);

            container.Add(topRow);

            if (page.tags == null || page.tags.Count == 0)
            {
                var tags = new Label("No tags");
                tags.AddToClassList("dn-list-subtitle");
                container.Add(tags);
            }
            else
            {
                container.Add(CreateTagRow(page.tags, 3, true));
            }

            if (_editingPageIndex == pageIndex)
            {
                container.Add(BuildSelectedPageCardEditor(page));
            }
            else
            {
                container.Add(CreatePageContextSummary(page));
            }

            return container;
        }

        private void RefreshEditor()
        {
            _pageEditorHost.Clear();

            if (_currentNotebook == null)
            {
                _pageEditorHost.Add(CreateEmptyState("No notebook loaded.", "Create a notebook or reopen recent work from Getting Started."));
                return;
            }

            if (_selectedSectionIndex < 0 || _selectedSectionIndex >= _currentNotebook.sections.Count)
            {
                _pageEditorHost.Add(CreateEmptyState("No section selected.", "Create a section to start capturing notes."));
                return;
            }

            DevNotebookSection section = _currentNotebook.sections[_selectedSectionIndex];
            if (_selectedPageIndex < 0 || _selectedPageIndex >= section.pages.Count)
            {
                _pageEditorHost.Add(CreateEmptyState("No page selected.", "Choose a page from the list or create a new one."));
                return;
            }

            DevNotebookPage page = section.pages[_selectedPageIndex];
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("dn-editor-scroll");
            _pageEditorHost.Add(scroll);

            scroll.Add(BuildPageOverviewCard(page));
            scroll.Add(BuildChecklistCard(page));
            scroll.Add(BuildSceneCard(page));
            scroll.Add(BuildLinkedAssetsCard(page));
        }

        private VisualElement BuildPageOverviewCard(DevNotebookPage page)
        {
            var card = CreateCard();

            var title = new Label("Page Overview");
            title.AddToClassList("dn-card-title");
            card.Add(title);

            if (!_isPageEditMode)
            {
                var pageTitle = new Label(string.IsNullOrWhiteSpace(page.title) ? "Untitled Page" : page.title);
                pageTitle.AddToClassList("dn-detail-title");
                card.Add(pageTitle);

                var statusLine = new Label($"Status: {GetStatusLabel(page.status)}");
                statusLine.AddToClassList("dn-card-copy");
                statusLine.AddToClassList(GetStatusClass(page.status));
                card.Add(statusLine);

                if (page.tags.Count > 0)
                {
                    card.Add(CreateTagRow(page.tags, page.tags.Count, false));
                }

                var body = new Label(string.IsNullOrWhiteSpace(page.body) ? "No page body yet." : page.body);
                body.AddToClassList("dn-card-copy");
                card.Add(body);
                return card;
            }

            var pageTitleField = new TextField("Page Title")
            {
                value = page.title,
                isDelayed = true
            };
            pageTitleField.RegisterValueChangedCallback(evt =>
            {
                MutatePage("Rename Page", page, () =>
                {
                    page.title = evt.newValue;
                }, refreshNotebookDetails: false, refreshHeader: false);
            });
            card.Add(pageTitleField);

            var statusField = new EnumField("Status", page.status);
            statusField.RegisterValueChangedCallback(evt =>
            {
                MutatePage("Change Page Status", page, () =>
                {
                    page.status = (DevNotebookPageStatus)evt.newValue;
                }, refreshNotebookDetails: false, refreshHeader: false);
            });
            card.Add(statusField);

            var tagsField = new TextField("Tags")
            {
                value = string.Join(", ", page.tags.Where(tag => !string.IsNullOrWhiteSpace(tag))),
                isDelayed = true
            };
            tagsField.RegisterValueChangedCallback(evt =>
            {
                MutatePage("Edit Page Tags", page, () =>
                {
                    page.tags = evt.newValue
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(tag => tag.Trim())
                        .Where(tag => !string.IsNullOrWhiteSpace(tag))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }, refreshNotebookDetails: false, refreshHeader: false);
            });
            card.Add(tagsField);

            var bodyField = new TextField("Body")
            {
                value = page.body,
                multiline = true,
                isDelayed = true
            };
            bodyField.AddToClassList("dn-body-field");
            bodyField.RegisterValueChangedCallback(evt =>
            {
                MutatePage("Edit Page Body", page, () =>
                {
                    page.body = evt.newValue;
                }, refreshNotebookDetails: false, refreshHeader: false, refreshPages: false);
            });
            card.Add(bodyField);

            return card;
        }

        private VisualElement BuildChecklistCard(DevNotebookPage page)
        {
            var card = CreateCard();

            var title = new Label("Checklist");
            title.AddToClassList("dn-card-title");
            card.Add(title);

            if (page.checklistItems.Count == 0)
            {
                card.Add(CreateInlineHint("No checklist items yet."));
            }

            for (int index = 0; index < page.checklistItems.Count; index++)
            {
                int localIndex = index;
                DevNotebookChecklistItem item = page.checklistItems[index];

                var row = new VisualElement();
                row.AddToClassList("dn-checklist-row");

                var toggle = new Toggle
                {
                    value = item.isComplete
                };
                if (!_isPageEditMode)
                {
                    toggle.SetEnabled(false);
                    row.Add(toggle);

                    var label = new Label(item.text);
                    label.AddToClassList("dn-grow");
                    row.Add(label);
                }
                else
                {
                    toggle.RegisterValueChangedCallback(evt =>
                    {
                        MutatePage("Toggle Checklist Item", page, () =>
                        {
                            item.isComplete = evt.newValue;
                        }, refreshNotebookDetails: false, refreshHeader: false, refreshPages: false);
                    });
                    row.Add(toggle);

                    var textField = new TextField
                    {
                        value = item.text,
                        isDelayed = true
                    };
                    textField.AddToClassList("dn-grow");
                    textField.AddToClassList("dn-inline-edit-field");
                    textField.RegisterValueChangedCallback(evt =>
                    {
                        MutatePage("Edit Checklist Item", page, () =>
                        {
                            item.text = evt.newValue;
                        }, refreshNotebookDetails: false, refreshHeader: false, refreshPages: false);
                    });
                    row.Add(textField);

                    row.Add(CreateButton("Remove", "dn-button-subtle", () =>
                    {
                        MutatePage("Remove Checklist Item", page, () =>
                        {
                            page.checklistItems.RemoveAt(localIndex);
                        }, refreshNotebookDetails: false, refreshHeader: false, refreshPages: false);
                    }));
                }

                card.Add(row);
            }

            if (!_isPageEditMode)
            {
                return card;
            }

            card.Add(CreateButton("Add Checklist Item", "dn-button-primary", () =>
            {
                MutatePage("Add Checklist Item", page, () =>
                {
                    page.checklistItems.Add(new DevNotebookChecklistItem());
                }, refreshNotebookDetails: false, refreshHeader: false, refreshPages: false);
            }));

            return card;
        }

        private VisualElement BuildSceneCard(DevNotebookPage page)
        {
            var card = CreateCard();

            var title = new Label("Linked Scene");
            title.AddToClassList("dn-card-title");
            card.Add(title);

            if (!_isPageEditMode)
            {
                SceneAsset sceneAsset = DevNotebookSceneLinkUtility.ResolveSceneAsset(page.linkedScene);
                if (sceneAsset == null)
                {
                    card.Add(CreateInlineHint("No scene linked yet."));
                    return card;
                }

                card.Add(CreateLinkTile(
                    sceneAsset,
                    string.IsNullOrWhiteSpace(page.linkedScene.sceneName) ? sceneAsset.name : page.linkedScene.sceneName,
                    "Scene link",
                    () => PingObject(sceneAsset)));

                var viewActions = new VisualElement();
                viewActions.AddToClassList("dn-card-actions");
                viewActions.Add(CreateButton("Open Linked Scene", "dn-button-subtle", () =>
                {
                    if (!DevNotebookSceneLinkUtility.OpenLinkedScene(page.linkedScene))
                    {
                        ShowNotification(new GUIContent("No linked scene could be opened."));
                    }
                }));
                viewActions.Add(CreateButton("Ping Scene", "dn-button-subtle", () => PingObject(sceneAsset)));
                card.Add(viewActions);
                return card;
            }

            var sceneField = new ObjectField("Scene")
            {
                objectType = typeof(SceneAsset),
                allowSceneObjects = false,
                value = DevNotebookSceneLinkUtility.ResolveSceneAsset(page.linkedScene)
            };
            sceneField.AddToClassList("dn-picker-field-wide");
            sceneField.RegisterValueChangedCallback(evt =>
            {
                MutatePage("Assign Scene Link", page, () =>
                {
                    DevNotebookSceneLinkUtility.AssignScene(page.linkedScene, evt.newValue as SceneAsset);
                }, refreshNotebookDetails: false, refreshHeader: false, refreshPages: true);
            });
            card.Add(sceneField);

            var actions = new VisualElement();
            actions.AddToClassList("dn-card-actions");
            actions.Add(CreateButton("Set Current Scene", "dn-button-primary", () =>
            {
                MutatePage("Assign Current Scene", page, () =>
                {
                    DevNotebookSceneLinkUtility.AssignCurrentScene(page.linkedScene);
                }, refreshNotebookDetails: false, refreshHeader: false);
            }));

            actions.Add(CreateButton("Open Linked Scene", "dn-button-subtle", () =>
            {
                if (!DevNotebookSceneLinkUtility.OpenLinkedScene(page.linkedScene))
                {
                    ShowNotification(new GUIContent("No linked scene could be opened."));
                }
            }));

            actions.Add(CreateButton("Clear", "dn-button-subtle", () =>
            {
                MutatePage("Clear Scene Link", page, () =>
                {
                    page.linkedScene.Clear();
                }, refreshNotebookDetails: false, refreshHeader: false);
            }));
            card.Add(actions);

            return card;
        }

        private VisualElement BuildLinkedAssetsCard(DevNotebookPage page)
        {
            var card = CreateCard();

            var title = new Label("Linked Assets");
            title.AddToClassList("dn-card-title");
            card.Add(title);

            var actions = new VisualElement();
            actions.AddToClassList("dn-inline-actions");
            actions.Add(CreateButton("Add Selected Assets", "dn-button-primary", () =>
            {
                MutatePage("Add Current Selection", page, () =>
                {
                    DevNotebookAssetLinkUtility.AddSelectionToPage(page, Selection.objects);
                }, refreshNotebookDetails: false, refreshHeader: false, refreshPages: false);
            }));

            if (_isPageEditMode)
            {
                actions.Add(CreateButton("Add Asset Slot", "dn-button-subtle", () =>
                {
                    MutatePage("Add Linked Asset Slot", page, () =>
                    {
                        page.linkedAssets.Add(null);
                    }, refreshNotebookDetails: false, refreshHeader: false, refreshPages: false);
                }));
            }

            card.Add(actions);

            if (page.linkedAssets.Count > 0)
            {
                card.Add(CreateInlineHint(page.linkedAssets.Count == 1
                    ? "1 linked asset"
                    : $"{page.linkedAssets.Count} linked assets"));
            }
            else
            {
                card.Add(CreateInlineHint("You can link multiple project assets to the same page."));
            }

            if (!_isPageEditMode)
            {
                if (page.linkedAssets.Count == 0)
                {
                    return card;
                }

                card.Add(CreateInlineHint("Click a tile to ping the linked asset."));

                var grid = new VisualElement();
                grid.AddToClassList("dn-link-grid");

                for (int index = 0; index < page.linkedAssets.Count; index++)
                {
                    UnityEngine.Object asset = page.linkedAssets[index];
                    string assetTitle = asset == null ? "Missing asset" : asset.name;
                    string subtitle = asset == null ? "Unavailable" : GetObjectTypeLabel(asset);
                    grid.Add(CreateLinkTile(asset, assetTitle, subtitle, () => PingObject(asset)));
                }

                card.Add(grid);

                return card;
            }

            if (page.linkedAssets.Count == 0)
            {
                return card;
            }

            for (int index = 0; index < page.linkedAssets.Count; index++)
            {
                int localIndex = index;
                var assetCard = new VisualElement();
                assetCard.AddToClassList("dn-asset-edit-card");

                var row = new VisualElement();
                row.AddToClassList("dn-asset-edit-head");

                row.Add(CreatePreviewSquare(page.linkedAssets[index], "dn-edit-preview"));

                var objectField = new ObjectField
                {
                    objectType = typeof(UnityEngine.Object),
                    allowSceneObjects = false,
                    value = page.linkedAssets[index]
                };
                objectField.AddToClassList("dn-picker-field");
                objectField.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue is SceneAsset)
                    {
                        objectField.SetValueWithoutNotify(page.linkedAssets[localIndex]);
                        return;
                    }

                    MutatePage("Edit Linked Asset", page, () =>
                    {
                        page.linkedAssets[localIndex] = evt.newValue;
                    }, refreshNotebookDetails: false, refreshHeader: false, refreshPages: false);
                });
                row.Add(objectField);
                assetCard.Add(row);

                var actionRow = new VisualElement();
                actionRow.AddToClassList("dn-card-actions");

                actionRow.Add(CreateButton("Ping", "dn-button-subtle", () =>
                {
                    PingObject(page.linkedAssets[localIndex]);
                }));

                actionRow.Add(CreateButton("Remove", "dn-button-subtle", () =>
                {
                    MutatePage("Remove Linked Asset", page, () =>
                    {
                        page.linkedAssets.RemoveAt(localIndex);
                    }, refreshNotebookDetails: false, refreshHeader: false, refreshPages: false);
                }));

                assetCard.Add(actionRow);
                card.Add(assetCard);
            }

            return card;
        }

        private VisualElement CreateListCard(bool selected, Action onClick)
        {
            var card = new VisualElement();
            card.AddToClassList("dn-list-card");
            if (selected)
            {
                card.AddToClassList("is-selected");
            }

            card.RegisterCallback<ClickEvent>(evt =>
            {
                onClick?.Invoke();
            });
            return card;
        }

        private void HandleSectionCardClick(int sectionIndex)
        {
            double now = EditorApplication.timeSinceStartup;
            bool isDoubleClick = _selectedSectionIndex == sectionIndex &&
                _lastSectionClickIndex == sectionIndex &&
                now - _lastSectionClickTime <= 0.35d;
            bool keepEditing = _editingSectionIndex == sectionIndex;

            _selectedSectionIndex = sectionIndex;
            _selectedPageIndex = _currentNotebook.sections[sectionIndex].pages.Count > 0 ? 0 : -1;
            _editingSectionIndex = isDoubleClick || keepEditing ? sectionIndex : -1;
            _lastSectionClickIndex = sectionIndex;
            _lastSectionClickTime = now;
            _editingPageIndex = -1;
            _lastPageClickIndex = -1;
            _lastPageClickTime = 0d;

            RefreshSections();
            RefreshPages();
            RefreshEditor();
        }

        private void HandlePageCardClick(int pageIndex)
        {
            double now = EditorApplication.timeSinceStartup;
            bool isDoubleClick = _selectedPageIndex == pageIndex &&
                _lastPageClickIndex == pageIndex &&
                now - _lastPageClickTime <= 0.35d;
            bool keepEditing = _editingPageIndex == pageIndex;

            _selectedPageIndex = pageIndex;
            _lastPageClickIndex = pageIndex;
            _lastPageClickTime = now;
            _editingPageIndex = isDoubleClick || keepEditing ? pageIndex : -1;
            RecordSelectedPage();

            RefreshPages();
            RefreshEditor();
            RefreshActionState();
        }

        private VisualElement CreateEmptyState(string titleText, string subtitleText)
        {
            var container = new VisualElement();
            container.AddToClassList("dn-empty-state");

            var title = new Label(titleText);
            title.AddToClassList("dn-empty-title");
            container.Add(title);

            var subtitle = new Label(subtitleText);
            subtitle.AddToClassList("dn-empty-copy");
            container.Add(subtitle);

            return container;
        }

        private VisualElement CreateInlineHint(string text)
        {
            var hint = new Label(text);
            hint.AddToClassList("dn-inline-hint");
            return hint;
        }

        private VisualElement BuildSelectedPageCardEditor(DevNotebookPage page)
        {
            var container = new VisualElement();
            container.AddToClassList("dn-page-inline-editor");
            container.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
            container.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

            var titleField = new TextField("Page Title")
            {
                value = page.title,
                isDelayed = true
            };
            titleField.RegisterValueChangedCallback(evt =>
            {
                MutatePage("Rename Page", page, () =>
                {
                    page.title = evt.newValue;
                }, refreshNotebookDetails: false, refreshHeader: false, refreshEditor: false);
            });
            container.Add(titleField);

            container.Add(CreateButton("Done", "dn-button-subtle", () =>
            {
                _editingPageIndex = -1;
                RefreshPages();
            }));

            return container;
        }

        private VisualElement CreatePageQuickActions(int pageIndex, DevNotebookPage page)
        {
            var row = new VisualElement();
            row.AddToClassList("dn-page-quick-actions");

            row.Add(CreateButton("Rename", "dn-button-subtle", () =>
            {
                _selectedPageIndex = pageIndex;
                _editingPageIndex = pageIndex;
                RecordSelectedPage();
                RefreshPages();
                RefreshActionState();
            }));

            string pinLabel = page.isPinned ? "Unpin" : "Pin";
            row.Add(CreateButton(pinLabel, "dn-button-subtle", () =>
            {
                _selectedPageIndex = pageIndex;
                TogglePagePin(pageIndex);
            }));

            if (!string.IsNullOrWhiteSpace(page.linkedScene?.sceneGuid) || !string.IsNullOrWhiteSpace(page.linkedScene?.scenePath))
            {
                row.Add(CreateButton("Open Scene", "dn-button-subtle", () =>
                {
                    if (!DevNotebookSceneLinkUtility.OpenLinkedScene(page.linkedScene))
                    {
                        ShowNotification(new GUIContent("No linked scene could be opened."));
                    }
                }));
            }
            else
            {
                UnityEngine.Object firstAsset = page.linkedAssets?.FirstOrDefault(asset => asset != null);
                if (firstAsset != null)
                {
                    row.Add(CreateButton("Ping Asset", "dn-button-subtle", () => PingObject(firstAsset)));
                }
            }

            return row;
        }

        private VisualElement CreatePageContextSummary(DevNotebookPage page)
        {
            SceneAsset sceneAsset = DevNotebookSceneLinkUtility.ResolveSceneAsset(page.linkedScene);
            if (sceneAsset != null)
            {
                return CreateCompactContextRow(
                    sceneAsset,
                    string.IsNullOrWhiteSpace(page.linkedScene.sceneName) ? sceneAsset.name : page.linkedScene.sceneName,
                    "Scene link");
            }

            UnityEngine.Object firstAsset = page.linkedAssets?.FirstOrDefault(asset => asset != null);
            if (firstAsset != null)
            {
                return CreateCompactContextRow(firstAsset, firstAsset.name, GetObjectTypeLabel(firstAsset));
            }

            string bodyPreview = GetBodyPreview(page.body);
            if (!string.IsNullOrWhiteSpace(bodyPreview))
            {
                var bodyLabel = new Label(bodyPreview);
                bodyLabel.AddToClassList("dn-page-body-preview");
                return bodyLabel;
            }

            var label = new Label("No linked scene or asset");
            label.AddToClassList("dn-list-subtitle");
            return label;
        }

        private VisualElement CreateSectionVisualization(DevNotebookSection section)
        {
            var container = new VisualElement();
            container.AddToClassList("dn-section-visual");

            List<UnityEngine.Object> previewObjects = GetSectionPreviewObjects(section, 3);
            if (previewObjects.Count == 0)
            {
                string emptyText = section.pages.Count == 0
                    ? "Add pages to build a section preview."
                    : "Link scenes or assets to preview this section.";
                var emptyLabel = new Label(emptyText);
                emptyLabel.AddToClassList("dn-section-visual-empty");
                container.Add(emptyLabel);
                return container;
            }

            var strip = new VisualElement();
            strip.AddToClassList("dn-section-preview-strip");
            foreach (UnityEngine.Object previewObject in previewObjects)
            {
                strip.Add(CreatePreviewSquare(previewObject, "dn-section-preview"));
            }

            container.Add(strip);

            int linkedPageCount = section.pages.Count(HasPageContextPreview);
            string summaryText = linkedPageCount == 1
                ? "1 page with linked context"
                : $"{linkedPageCount} pages with linked context";
            var summary = new Label(summaryText);
            summary.AddToClassList("dn-section-visual-summary");
            container.Add(summary);

            return container;
        }

        private List<UnityEngine.Object> GetSectionPreviewObjects(DevNotebookSection section, int maxCount)
        {
            var previews = new List<UnityEngine.Object>();
            var seenInstanceIds = new HashSet<int>();

            if (section?.pages == null)
            {
                return previews;
            }

            foreach (DevNotebookPage page in section.pages)
            {
                UnityEngine.Object previewObject = GetPageContextObject(page);
                if (previewObject == null)
                {
                    continue;
                }

                int instanceId = previewObject.GetInstanceID();
                if (!seenInstanceIds.Add(instanceId))
                {
                    continue;
                }

                previews.Add(previewObject);
                if (previews.Count >= maxCount)
                {
                    break;
                }
            }

            return previews;
        }

        private bool HasPageContextPreview(DevNotebookPage page)
        {
            return GetPageContextObject(page) != null;
        }

        private UnityEngine.Object GetPageContextObject(DevNotebookPage page)
        {
            if (page == null)
            {
                return null;
            }

            SceneAsset sceneAsset = DevNotebookSceneLinkUtility.ResolveSceneAsset(page.linkedScene);
            if (sceneAsset != null)
            {
                return sceneAsset;
            }

            return page.linkedAssets?.FirstOrDefault(asset => asset != null);
        }

        private static string GetBodyPreview(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }

            string normalized = body.Replace("\r", " ").Replace("\n", " ").Trim();
            if (normalized.Length <= 72)
            {
                return normalized;
            }

            return $"{normalized.Substring(0, 72).TrimEnd()}...";
        }

        private VisualElement CreateCompactContextRow(UnityEngine.Object asset, string titleText, string subtitleText)
        {
            var row = new VisualElement();
            row.AddToClassList("dn-page-context-row");
            row.Add(CreatePreviewSquare(asset, "dn-page-context-preview"));

            var copy = new VisualElement();
            copy.AddToClassList("dn-page-context-copy");

            var title = new Label(titleText);
            title.AddToClassList("dn-page-context-title");
            copy.Add(title);

            var subtitle = new Label(subtitleText);
            subtitle.AddToClassList("dn-page-context-subtitle");
            copy.Add(subtitle);

            row.Add(copy);
            return row;
        }

        private VisualElement CreateLinkTile(UnityEngine.Object asset, string titleText, string subtitleText, Action onClick)
        {
            var tile = new VisualElement();
            tile.AddToClassList("dn-link-tile");
            tile.tooltip = asset == null ? "Asset reference is missing." : titleText;

            tile.Add(CreatePreviewSquare(asset, "dn-link-tile-preview"));

            var title = new Label(titleText);
            title.AddToClassList("dn-link-tile-title");
            tile.Add(title);

            var subtitle = new Label(subtitleText);
            subtitle.AddToClassList("dn-link-tile-subtitle");
            tile.Add(subtitle);

            if (asset != null && onClick != null)
            {
                tile.RegisterCallback<ClickEvent>(_ => onClick.Invoke());
            }

            return tile;
        }

        private VisualElement CreatePreviewSquare(UnityEngine.Object asset, string className)
        {
            var shell = new VisualElement();
            shell.AddToClassList("dn-preview-square");
            shell.AddToClassList(className);

            Texture previewTexture = GetPreviewTexture(asset);
            if (previewTexture != null)
            {
                var image = new Image
                {
                    image = previewTexture,
                    scaleMode = ScaleMode.ScaleToFit
                };
                image.AddToClassList("dn-preview-image");
                shell.Add(image);
                return shell;
            }

            string fallbackText = "?";
            if (asset != null && !string.IsNullOrWhiteSpace(asset.name))
            {
                fallbackText = asset.name.Substring(0, 1).ToUpperInvariant();
            }

            var fallback = new Label(fallbackText);
            fallback.AddToClassList("dn-preview-fallback");
            shell.Add(fallback);
            return shell;
        }

        private static Texture GetPreviewTexture(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return EditorGUIUtility.IconContent("console.warnicon").image;
            }

            return AssetPreview.GetAssetPreview(asset)
                ?? AssetPreview.GetMiniThumbnail(asset)
                ?? EditorGUIUtility.ObjectContent(asset, asset.GetType()).image;
        }

        private static string GetObjectTypeLabel(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return "Missing reference";
            }

            return ObjectNames.NicifyVariableName(asset.GetType().Name);
        }

        private static void PingObject(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return;
            }

            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }

        private Label CreatePinMarker()
        {
            var pinMarker = new Label("\u2605");
            pinMarker.AddToClassList("dn-pin-marker");
            pinMarker.tooltip = "Pinned";
            return pinMarker;
        }

        private VisualElement CreateTagRow(IEnumerable<string> tags, int maxCount, bool compact)
        {
            var row = new VisualElement();
            row.AddToClassList("dn-tag-row");

            foreach (string tag in tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Take(maxCount))
            {
                Label tagLabel = CreateTagPill(tag, compact);
                row.Add(tagLabel);
            }

            return row;
        }

        private Label CreateTagPill(string tag, bool compact)
        {
            var label = new Label(tag);
            label.AddToClassList("dn-pill");

            if (compact)
            {
                label.AddToClassList("dn-pill-compact");
            }

            return label;
        }

        private void BuildSectionListContextMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("New Section", _ => CreateSection(), _ =>
                _currentNotebook != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            if (_selectedSectionIndex >= 0 && _currentNotebook != null && _selectedSectionIndex < _currentNotebook.sections.Count)
            {
                string pinLabel = _currentNotebook.sections[_selectedSectionIndex].isPinned
                    ? "Unpin Selected Section"
                    : "Pin Selected Section";
                evt.menu.AppendAction(pinLabel, _ => ToggleSectionPin(_selectedSectionIndex), _ => DropdownMenuAction.Status.Normal);
                evt.menu.AppendAction("Delete Selected Section", _ => DeleteSelectedSection(), _ => DropdownMenuAction.Status.Normal);
            }
        }

        private void BuildPageListContextMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("New Page", _ => CreatePage(), _ =>
                _currentNotebook != null && _selectedSectionIndex >= 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            if (TryGetSelectedPage(out _))
            {
                DevNotebookPage selectedPage = _currentNotebook.sections[_selectedSectionIndex].pages[_selectedPageIndex];
                string pinLabel = selectedPage.isPinned ? "Unpin Selected Page" : "Pin Selected Page";
                string pageModeLabel = _isPageEditMode ? "View Selected Page" : "Edit Selected Page";
                evt.menu.AppendAction(pageModeLabel, _ => ToggleSelectedPageMode(), _ => DropdownMenuAction.Status.Normal);
                evt.menu.AppendAction("Rename Selected Page", _ =>
                {
                    _editingPageIndex = _selectedPageIndex;
                    RefreshPages();
                }, _ => DropdownMenuAction.Status.Normal);
                evt.menu.AppendAction(pinLabel, _ => TogglePagePin(_selectedPageIndex), _ => DropdownMenuAction.Status.Normal);
                evt.menu.AppendAction("Delete Selected Page", _ => DeleteSelectedPage(), _ => DropdownMenuAction.Status.Normal);
            }
        }

        private void AttachSectionCardContextMenu(VisualElement card, int sectionIndex)
        {
            card.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.StopPropagation();

                evt.menu.AppendAction("Edit Section", _ =>
                {
                    _selectedSectionIndex = sectionIndex;
                    _editingSectionIndex = sectionIndex;
                    _selectedPageIndex = _currentNotebook.sections[sectionIndex].pages.Count > 0 ? 0 : -1;
                    RefreshSections();
                    RefreshPages();
                    RefreshEditor();
                });

                string pinLabel = _currentNotebook.sections[sectionIndex].isPinned ? "Unpin Section" : "Pin Section";
                evt.menu.AppendAction(pinLabel, _ => ToggleSectionPin(sectionIndex));

                evt.menu.AppendAction("Delete Section", _ =>
                {
                    _selectedSectionIndex = sectionIndex;
                    _editingSectionIndex = -1;
                    DeleteSelectedSection();
                });
            }));
        }

        private void AttachPageCardContextMenu(VisualElement card, int pageIndex)
        {
            card.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.StopPropagation();

                evt.menu.AppendAction("Open Page", _ =>
                {
                    _selectedPageIndex = pageIndex;
                    _editingPageIndex = -1;
                    RecordSelectedPage();
                    RefreshPages();
                    RefreshEditor();
                });

                evt.menu.AppendAction("Rename Page", _ =>
                {
                    _selectedPageIndex = pageIndex;
                    _editingPageIndex = pageIndex;
                    RefreshPages();
                });

                string pinLabel = _currentNotebook.sections[_selectedSectionIndex].pages[pageIndex].isPinned ? "Unpin Page" : "Pin Page";
                evt.menu.AppendAction(pinLabel, _ =>
                {
                    _selectedPageIndex = pageIndex;
                    TogglePagePin(pageIndex);
                });

                evt.menu.AppendAction("Delete Page", _ =>
                {
                    _selectedPageIndex = pageIndex;
                    DeleteSelectedPage();
                });
            }));
        }

        private void CreateSection()
        {
            if (_currentNotebook == null)
            {
                DevNotebookNotebook notebook = DevNotebookCreationUtility.CreateNotebookInSelectedFolder(true);
                if (notebook == null)
                {
                    return;
                }

                _currentNotebookGuid = DevNotebookNotebookRepository.GetGuid(notebook);
                EnsureNotebookLoaded();
            }

            MutateNotebook("Add Section", () =>
            {
                _currentNotebook.sections.Add(new DevNotebookSection
                {
                    title = $"Section {_currentNotebook.sections.Count + 1}"
                });
                _selectedSectionIndex = _currentNotebook.sections.Count - 1;
                _editingSectionIndex = _selectedSectionIndex;
                _selectedPageIndex = -1;
            });
        }

        private void DeleteSelectedSection()
        {
            if (_currentNotebook == null || _selectedSectionIndex < 0 || _selectedSectionIndex >= _currentNotebook.sections.Count)
            {
                return;
            }

            if (!EditorUtility.DisplayDialog("Delete Section", "Delete the selected section and all of its pages?", "Delete", "Cancel"))
            {
                return;
            }

            MutateNotebook("Delete Section", () =>
            {
                _currentNotebook.sections.RemoveAt(_selectedSectionIndex);
                _selectedSectionIndex = Mathf.Clamp(_selectedSectionIndex - 1, 0, _currentNotebook.sections.Count - 1);
                if (_currentNotebook.sections.Count == 0)
                {
                    _selectedSectionIndex = -1;
                    _editingSectionIndex = -1;
                    _selectedPageIndex = -1;
                }
                else
                {
                    _editingSectionIndex = -1;
                    _selectedPageIndex = _currentNotebook.sections[_selectedSectionIndex].pages.Count > 0 ? 0 : -1;
                }
            });
        }

        private void CreatePage()
        {
            if (_currentNotebook == null)
            {
                return;
            }

            if (_selectedSectionIndex < 0)
            {
                CreateSection();
                if (_selectedSectionIndex < 0)
                {
                    return;
                }
            }

            MutateNotebook("Add Page", () =>
            {
                DevNotebookSection section = _currentNotebook.sections[_selectedSectionIndex];
                section.pages.Add(new DevNotebookPage
                {
                    title = $"Page {section.pages.Count + 1}"
                });
                _selectedPageIndex = section.pages.Count - 1;
                _editingPageIndex = _selectedPageIndex;
                _isPageEditMode = true;
                _lastPageClickIndex = -1;
                _lastPageClickTime = 0d;
            });
        }

        private void DeleteSelectedPage()
        {
            if (!TryGetSelectedPage(out DevNotebookPage _))
            {
                return;
            }

            if (!EditorUtility.DisplayDialog("Delete Page", "Delete the selected page?", "Delete", "Cancel"))
            {
                return;
            }

            MutateNotebook("Delete Page", () =>
            {
                DevNotebookSection section = _currentNotebook.sections[_selectedSectionIndex];
                section.pages.RemoveAt(_selectedPageIndex);
                _selectedPageIndex = section.pages.Count == 0 ? -1 : Mathf.Clamp(_selectedPageIndex - 1, 0, section.pages.Count - 1);
                _editingPageIndex = -1;
                _lastPageClickIndex = -1;
                _lastPageClickTime = 0d;
            });
        }

        private void EnterSelectedPageEditMode()
        {
            if (!TryGetSelectedPage(out _))
            {
                return;
            }

            _editingPageIndex = -1;
            _isPageEditMode = true;
            RefreshEditor();
            RefreshActionState();
        }

        private void EnterSelectedPageViewMode()
        {
            if (!TryGetSelectedPage(out _))
            {
                return;
            }

            _isPageEditMode = false;
            RefreshEditor();
            RefreshActionState();
        }

        private void ToggleSelectedPageMode()
        {
            if (_isPageEditMode)
            {
                EnterSelectedPageViewMode();
                return;
            }

            EnterSelectedPageEditMode();
        }

        private void ToggleSectionPin(int sectionIndex)
        {
            if (_currentNotebook == null || sectionIndex < 0 || sectionIndex >= _currentNotebook.sections.Count)
            {
                return;
            }

            MutateNotebook("Toggle Section Pin", () =>
            {
                _currentNotebook.sections[sectionIndex].isPinned = !_currentNotebook.sections[sectionIndex].isPinned;
            }, refreshPages: false, refreshEditor: false, refreshNotebookDetails: false, refreshHeader: false);
        }

        private void TogglePagePin(int pageIndex)
        {
            if (!TryGetSelectedPage(out _) ||
                pageIndex < 0 ||
                _selectedSectionIndex < 0 ||
                _selectedSectionIndex >= _currentNotebook.sections.Count)
            {
                return;
            }

            DevNotebookSection section = _currentNotebook.sections[_selectedSectionIndex];
            if (pageIndex >= section.pages.Count)
            {
                return;
            }

            MutateNotebook("Toggle Page Pin", () =>
            {
                section.pages[pageIndex].isPinned = !section.pages[pageIndex].isPinned;
            }, refreshSections: false, refreshNotebookDetails: false, refreshHeader: false);
        }

        private void OnNotebookDropdownChanged(ChangeEvent<string> evt)
        {
            if (_notebookGuids.Count == 0)
            {
                return;
            }

            int index = _notebookDropdown.choices.IndexOf(evt.newValue);
            if (index < 0 || index >= _notebookGuids.Count)
            {
                return;
            }

            _currentNotebookGuid = _notebookGuids[index];
            _selectedSectionIndex = -1;
            _selectedPageIndex = -1;
            _editingPageIndex = -1;
            _lastPageClickIndex = -1;
            _lastPageClickTime = 0d;
            RefreshAll();
        }

        private void MutateNotebook(
            string undoName,
            Action mutate,
            bool refreshSections = true,
            bool refreshPages = true,
            bool refreshEditor = true,
            bool refreshNotebookDetails = true,
            bool refreshHeader = true)
        {
            if (_currentNotebook == null || mutate == null)
            {
                return;
            }

            Undo.RecordObject(_currentNotebook, undoName);
            mutate.Invoke();
            _currentNotebook.EnsureData();
            EditorUtility.SetDirty(_currentNotebook);
            AssetDatabase.SaveAssetIfDirty(_currentNotebook);

            if (refreshHeader)
            {
                RefreshHeader();
            }

            ReloadNotebookOptions();

            if (refreshNotebookDetails)
            {
                RefreshNotebookDetails();
            }

            if (refreshSections)
            {
                RefreshSections();
            }

            if (refreshPages)
            {
                RefreshPages();
            }

            if (refreshEditor)
            {
                RefreshEditor();
            }

            RefreshActionState();
        }

        private void MutatePage(
            string undoName,
            DevNotebookPage page,
            Action mutate,
            bool refreshSections = false,
            bool refreshPages = true,
            bool refreshEditor = true,
            bool refreshNotebookDetails = false,
            bool refreshHeader = false)
        {
            MutateNotebook(undoName, () =>
            {
                mutate.Invoke();
                page.Touch();
                RecordSelectedPage();
            }, refreshSections, refreshPages, refreshEditor, refreshNotebookDetails, refreshHeader);
        }

        private void RecordSelectedPage()
        {
            if (!TryGetSelectedPage(out DevNotebookPage page))
            {
                return;
            }

            DevNotebookProjectSettings.instance.RecordPageOpened(
                _currentNotebookGuid,
                _currentNotebook.title,
                _selectedSectionIndex,
                _selectedPageIndex,
                page.title);
        }

        private bool TryGetSelectedPage(out DevNotebookPage page)
        {
            page = null;

            if (_currentNotebook == null ||
                _selectedSectionIndex < 0 ||
                _selectedSectionIndex >= _currentNotebook.sections.Count)
            {
                return false;
            }

            DevNotebookSection section = _currentNotebook.sections[_selectedSectionIndex];
            if (_selectedPageIndex < 0 || _selectedPageIndex >= section.pages.Count)
            {
                return false;
            }

            page = section.pages[_selectedPageIndex];
            return page != null;
        }

        private List<PageReference> GetFilteredPages()
        {
            var results = new List<PageReference>();

            if (_currentNotebook == null ||
                _selectedSectionIndex < 0 ||
                _selectedSectionIndex >= _currentNotebook.sections.Count)
            {
                return results;
            }

            DevNotebookSection section = _currentNotebook.sections[_selectedSectionIndex];
            DevNotebookPageStatus? statusFilter = GetStatusFilter();
            string sceneGuidFilter = _currentSceneOnly ? GetActiveSceneGuid() : string.Empty;

            for (int index = 0; index < section.pages.Count; index++)
            {
                DevNotebookPage page = section.pages[index];
                if (!DevNotebookPageFilter.Matches(page, _searchText, statusFilter, sceneGuidFilter))
                {
                    continue;
                }

                results.Add(new PageReference(index, page));
            }

            return results
                .OrderByDescending(reference => reference.Page.isPinned)
                .ThenBy(reference => reference.PageIndex)
                .ToList();
        }

        private IEnumerable<SectionReference> GetOrderedSections()
        {
            if (_currentNotebook == null)
            {
                return Enumerable.Empty<SectionReference>();
            }

            return _currentNotebook.sections
                .Select((section, index) => new SectionReference(index, section))
                .OrderByDescending(reference => reference.Section.isPinned)
                .ThenBy(reference => reference.SectionIndex);
        }

        private DevNotebookPageStatus? GetStatusFilter()
        {
            return _statusFilterIndex switch
            {
                1 => DevNotebookPageStatus.None,
                2 => DevNotebookPageStatus.Todo,
                3 => DevNotebookPageStatus.InProgress,
                4 => DevNotebookPageStatus.Done,
                5 => DevNotebookPageStatus.Blocked,
                _ => null
            };
        }

        private static string GetStatusLabel(DevNotebookPageStatus status)
        {
            return status switch
            {
                DevNotebookPageStatus.InProgress => "In Progress",
                _ => status.ToString()
            };
        }

        private static string GetStatusClass(DevNotebookPageStatus status)
        {
            return status switch
            {
                DevNotebookPageStatus.None => "status-none",
                DevNotebookPageStatus.Todo => "status-todo",
                DevNotebookPageStatus.InProgress => "status-inprogress",
                DevNotebookPageStatus.Done => "status-done",
                DevNotebookPageStatus.Blocked => "status-blocked",
                _ => "status-none"
            };
        }

        private static string GetActiveSceneGuid()
        {
            string path = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            return string.IsNullOrWhiteSpace(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
        }

        private void ToggleNotebookEditMode()
        {
            _isNotebookEditMode = !_isNotebookEditMode;
            RefreshNotebookDetails();
            RefreshActionState();
        }

        private void RefreshActionState()
        {
            if (_modeToggleButton != null)
            {
                _modeToggleButton.text = _isNotebookEditMode ? "View Notebook" : "Edit Notebook";
            }

            bool hasNotebook = _currentNotebook != null;
            bool hasSection = hasNotebook && _selectedSectionIndex >= 0 && _selectedSectionIndex < _currentNotebook.sections.Count;
            bool hasPage = TryGetSelectedPage(out _);

            if (_createSectionButton != null)
            {
                _createSectionButton.SetEnabled(true);
            }

            if (_deleteSectionButton != null)
            {
                _deleteSectionButton.SetEnabled(hasSection);
            }

            if (_createPageButton != null)
            {
                _createPageButton.SetEnabled(hasSection);
            }

            if (_editPageButton != null)
            {
                _editPageButton.text = _isPageEditMode ? "View Page" : "Edit Page";
                _editPageButton.SetEnabled(hasPage);
            }

            if (_deletePageButton != null)
            {
                _deletePageButton.SetEnabled(hasPage);
            }
        }

        private VisualElement CreateNotebookMetric(string labelText, string valueText)
        {
            var metric = new VisualElement();
            metric.AddToClassList("dn-header-metric");

            var label = new Label(labelText);
            label.AddToClassList("dn-header-metric-label");
            metric.Add(label);

            var value = new Label(valueText);
            value.AddToClassList("dn-header-metric-value");
            metric.Add(value);

            return metric;
        }

        private int GetTotalPageCount()
        {
            return _currentNotebook?.sections?.Sum(section => section?.pages?.Count ?? 0) ?? 0;
        }

        private readonly struct PageReference
        {
            public PageReference(int pageIndex, DevNotebookPage page)
            {
                PageIndex = pageIndex;
                Page = page;
            }

            public int PageIndex { get; }
            public DevNotebookPage Page { get; }
        }

        private readonly struct SectionReference
        {
            public SectionReference(int sectionIndex, DevNotebookSection section)
            {
                SectionIndex = sectionIndex;
                Section = section;
            }

            public int SectionIndex { get; }
            public DevNotebookSection Section { get; }
        }
    }
}
