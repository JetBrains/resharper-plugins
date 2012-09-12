/*
 * Copyright 2012 JetBrains s.r.o.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Drawing;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.TextControl;
using JetBrains.TextControl.Markup;
using JetBrains.Threading;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.ShowMvcVerbs
{
    [ShellComponent]
    public class ClassUsageTextStyleProvider
    {
        private readonly IThreading threading;
        private readonly IHighlighterCustomization highlighterCustomization;
        private readonly HighlightingSettingsManager highlightingSettingsManager;
        private readonly IContextBoundSettingsStore settingsStore;
        private TextStyle resharperClassIdentifier;
        private TextStyle userTypesIdentifier;

        public ClassUsageTextStyleProvider(Lifetime lifetime, IThreading threading, IHighlighterCustomization highlighterCustomization,
                                           DefaultTextControlSchemeManager textControlSchemeManager, HighlightingSettingsManager highlightingSettingsManager, 
                                           ISettingsStore settingsStore)
        {
            this.threading = threading;
            this.highlighterCustomization = highlighterCustomization;
            this.highlightingSettingsManager = highlightingSettingsManager;
            this.settingsStore = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide);

            textControlSchemeManager.ColorsChanged.Advise(lifetime, Refresh);
        }

        public void Refresh()
        {
            // We can only apply IDE customisations (including VS2012's light and dark theme changes) on the
            // main UI thread
            threading.ExecuteOrQueue("highlighter customisations", () =>
                {
                    // We want the colour used in the editor for the attributes at the start of the method. This is
                    // either "ReSharper Class Identifier" if ReSharper's "color identifiers" option is enabled, or
                    // Visual Studio's "User Types". We can ask ReSharper's IHighlightingAttributeRegistry for the
                    // list of registered highlights, but this returns a static list that doesn't include any changes
                    // made in Visual Studio's Fonts and Colours dialog, and doesn't reflect changes due to choosing
                    // the light or dark theme in VS2012. So we use IHighlighterCustomization to get the highlight and
                    // then update it with the current values (customisations) from the IDE.
                    // Things are slightly complicated by the fact that "User Types" isn't a registered highlight with
                    // ReSharper (it doesn't use it at all), so IHighlighterCustomization fails to retrieve a highlight
                    // in the first place. So we circumvent that section, give it a simple, "fake" highlight with the
                    // right name, and it dumbly asks the IDE for the correct values for "User Types" and applies it
                    // back to our highlight. Alternatively, we could register "User Types" as a highlight, and have
                    // it available to everyone, but there's no clean way to query existing highlights 
                    resharperClassIdentifier = GetRegisteredForeColour(highlighterCustomization, HighlightingAttributeIds.TYPE_CLASS_ATTRIBUTE);
                    userTypesIdentifier = GetVisualStudioForeColour(highlighterCustomization, new FakeHighlighter("User Types", Color.DarkOliveGreen));
                });
        }

        private static TextStyle GetRegisteredForeColour(IHighlighterCustomization highlighterCustomization, string id)
        {
            var highlighterAttributes = highlighterCustomization.GetCustomizedRegisteredHighlighterAttributes(id);
            return TextStyle.FromForeColor(highlighterAttributes.Color);
        }

        private static TextStyle GetVisualStudioForeColour(IHighlighterCustomization highlighterCustomization, IHighlighter highlighter)
        {
            var highlighterAttributes = highlighterCustomization.GetCustomizedHighlighterAttributes(highlighter);
            return TextStyle.FromForeColor(highlighterAttributes.Color);
        }

        public TextStyle GetClassUsageTextStyle()
        {
            return IsIdentifierHighlightingEnabled() ? resharperClassIdentifier : userTypesIdentifier;
        }

        private bool IsIdentifierHighlightingEnabled()
        {
            return highlightingSettingsManager.GetIdentifierHighlightingEnabled(settingsStore);
        }

        private class FakeHighlighter : IHighlighter
        {
            public FakeHighlighter(string id, Color defaultColour)
            {
                AttributeId = id;
                Attributes = new HighlighterAttributes(defaultColour);
            }

            public TextRange Range { get; private set; }
            public bool IsValid { get; private set; }
            public IDocument Document { get; private set; }
            public object UserData { get; set; }
            public Key Key { get; private set; }
            public int Layer { get; private set; }
            public AreaType AreaType { get; private set; }
            public string AttributeId { get; private set; }
            public HighlighterAttributes Attributes { get; private set; }
            public ErrorStripeAttributes ErrorStripeAttributes { get; private set; }
            public IGutterMark GutterMark { get; private set; }
            public string ToolTip { get; private set; }
            public string ErrorStripeToolTip { get; private set; }
            public RichTextBlock RichTextToolTip { get; private set; }
        }
    }
}