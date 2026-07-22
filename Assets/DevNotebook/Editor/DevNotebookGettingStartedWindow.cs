using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DevNotebook.Editor
{
    internal sealed class DevNotebookGettingStartedWindow : EditorWindow
    {
        public static DevNotebookGettingStartedWindow ShowWindow()
        {
            DevNotebookGettingStartedWindow window = GetWindow<DevNotebookGettingStartedWindow>(true, "DevNotebook Getting Started");
            window.minSize = new Vector2(860f, 680f);
            window.Show();
            return window;
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();

            StyleSheet styleSheet = DevNotebookEditorPaths.LoadStyleSheet();
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            rootVisualElement.AddToClassList("dn-root");
            rootVisualElement.AddToClassList(EditorGUIUtility.isProSkin ? "theme-dark" : "theme-light");

            var shell = new ScrollView();
            shell.AddToClassList("dn-shell");
            shell.AddToClassList("dn-start-shell");
            rootVisualElement.Add(shell);

            var eyebrow = new Label("DevNotebook");
            eyebrow.AddToClassList("dn-eyebrow");
            shell.Add(eyebrow);

            var heroTitle = new Label("Open notes that stay attached to your project");
            heroTitle.AddToClassList("dn-hero-title");
            shell.Add(heroTitle);

            var heroCopy = new Label("Reopen recent notebook work, create a fresh notebook, or import starter content when you want a quick example of the workflow.");
            heroCopy.AddToClassList("dn-hero-copy");
            shell.Add(heroCopy);

            shell.Add(BuildRecentWorkCard());
            shell.Add(BuildQuickStartCard());
            shell.Add(BuildWorkflowCard());
            shell.Add(BuildFooter());
        }

        private VisualElement BuildRecentWorkCard()
        {
            DevNotebookProjectSettings settings = DevNotebookProjectSettings.instance;
            settings.PruneInvalidEntries();

            var card = CreateCard("Recent Work", "Reopen recent notebooks and pages so the window feels useful instead of generic.");

            if (settings.RecentItems.Count == 0)
            {
                card.Add(CreateHint("No notebooks yet. Create one and DevNotebook will keep the recent work here."));
            }
            else
            {
                foreach (DevNotebookRecentItem item in settings.RecentItems.Take(4))
                {
                    string label = string.IsNullOrWhiteSpace(item.pageTitle)
                        ? item.notebookTitle
                        : $"{item.notebookTitle} - {item.pageTitle}";

                    card.Add(CreateButton(label, "dn-button-row", () =>
                    {
                        DevNotebookWindow.ShowWindow(item.notebookGuid, item.sectionIndex, item.pageIndex);
                        Close();
                    }));
                }
            }

            return card;
        }

        private VisualElement BuildQuickStartCard()
        {
            var card = CreateCard("Quick Start", "Create one notebook, add a section and page, then link the current scene or your selected assets.");
            card.Add(CreateHint("Use this when you are in the middle of a review pass and want notes to stay near the exact content you were inspecting."));
            return card;
        }

        private VisualElement BuildWorkflowCard()
        {
            var card = CreateCard("Learn the Workflow", "Sections group related notes, pages capture detail, and links bring scenes and assets back into reach.");
            card.Add(CreateHint("Use status chips, tags, and checklist items for lightweight tracking. The sample notebook shows the shape without forcing a heavy process."));
            return card;
        }

        private VisualElement BuildFooter()
        {
            var footer = new VisualElement();
            footer.AddToClassList("dn-card");

            var primaryActionGuid = DevNotebookStartupLogic.DetermineAction(
                true,
                true,
                DevNotebookProjectSettings.instance.lastOpenedNotebookGuid,
                DevNotebookProjectSettings.instance.RecentItems,
                DevNotebookNotebookRepository.FindNotebookGuids()).NotebookGuid;

            bool hasRecentNotebook = !string.IsNullOrWhiteSpace(primaryActionGuid);

            var buttonRow = new VisualElement();
            buttonRow.AddToClassList("dn-inline-actions");
            footer.Add(buttonRow);

            buttonRow.Add(CreateButton(hasRecentNotebook ? "Open Recent Notebook" : "Create Notebook", "dn-button-primary", () =>
            {
                if (hasRecentNotebook)
                {
                    DevNotebookWindow.ShowWindow(primaryActionGuid);
                }
                else
                {
                    DevNotebookNotebook notebook = DevNotebookCreationUtility.CreateNotebookInSelectedFolder(true);
                    if (notebook != null)
                    {
                        DevNotebookWindow.ShowWindow(DevNotebookNotebookRepository.GetGuid(notebook));
                    }
                }

                Close();
            }));

            buttonRow.Add(CreateButton("Open Sample", "dn-button-subtle", () =>
            {
                DevNotebookSampleImporter.ImportAndOpenStarterNotebook();
                Close();
            }));
            buttonRow.Add(CreateButton("Documentation", "dn-button-subtle", DevNotebookDocumentationUtility.OpenReadme));
            buttonRow.Add(CreateButton("Project Settings", "dn-button-subtle", () => SettingsService.OpenProjectSettings(DevNotebookEditorPaths.SettingsPath)));

            var toggle = new Toggle("Show on project load")
            {
                value = DevNotebookProjectSettings.instance.showGettingStartedOnLoad
            };
            toggle.RegisterValueChangedCallback(evt =>
            {
                DevNotebookProjectSettings.instance.showGettingStartedOnLoad = evt.newValue;
                DevNotebookProjectSettings.instance.SaveSettings();
            });
            footer.Add(toggle);

            return footer;
        }

        private VisualElement CreateCard(string titleText, string copyText)
        {
            var card = new VisualElement();
            card.AddToClassList("dn-card");

            var title = new Label(titleText);
            title.AddToClassList("dn-card-title");
            card.Add(title);

            var copy = new Label(copyText);
            copy.AddToClassList("dn-card-copy");
            card.Add(copy);

            return card;
        }

        private Button CreateButton(string text, string className, System.Action onClick)
        {
            var button = new Button(() => onClick?.Invoke())
            {
                text = text
            };
            button.AddToClassList(className);
            return button;
        }

        private Label CreateHint(string text)
        {
            var label = new Label(text);
            label.AddToClassList("dn-inline-hint");
            return label;
        }
    }
}
