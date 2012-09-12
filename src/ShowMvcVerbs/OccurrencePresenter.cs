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

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.Occurences;
using JetBrains.ReSharper.Feature.Services.Occurences.Presentation;
using JetBrains.ReSharper.Feature.Services.Occurences.Presentation.Presenters;
using JetBrains.ReSharper.Feature.Services.Search;
using JetBrains.ReSharper.Psi;
using JetBrains.UI.PopupMenu;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.ShowMvcVerbs
{
    [OccurencePresenter]
    public class OccurrencePresenter : DeclaredElementOccurencePresenter
    {
        private readonly ClassUsageTextStyleProvider textStyleProvider;

        public OccurrencePresenter(ClassUsageTextStyleProvider textStyleProvider)
        {
            this.textStyleProvider = textStyleProvider;
        }

        public override bool IsApplicable(IOccurence occurence)
        {
            return GetActionMethodSelectorAttributes(occurence).Any();
        }

        protected override void DisplayMainText(IMenuItemDescriptor descriptor, IOccurence occurence, OccurencePresentationOptions options, IDeclaredElement declaredElement)
        {
            base.DisplayMainText(descriptor, occurence, options, declaredElement);

            var text = GetText(occurence);
            if (text.Length > 0)
            {
                var textStyle = textStyleProvider.GetClassUsageTextStyle();


                TextStyle.FromForeColor(Color.FromArgb(78, 201, 176));
                var richText = new RichText(string.Format("[{0}] ", text), textStyle);
                richText.Append(descriptor.Text);
                descriptor.Text = richText;
            }
        }

        private static string GetText(IOccurence occurence)
        {
            var verbs = from attribute in GetActionMethodSelectorAttributes(occurence)
                        let verb = attribute.GetClrName().ShortName
                        select verb.Replace("Attribute", string.Empty);
            return string.Join(", ", verbs.ToArray());
        }

        private static IEnumerable<IAttributeInstance> GetActionMethodSelectorAttributes(IOccurence occurence)
        {
            var declaredElementOccurence = occurence as DeclaredElementOccurence;
            if (declaredElementOccurence == null)
                return EmptyList<IAttributeInstance>.InstanceList;

            var method = declaredElementOccurence.DisplayElement.GetValidDeclaredElement() as IMethod;
            if (method == null)
                return EmptyList<IAttributeInstance>.InstanceList;

            return from attribute in method.GetAttributeInstances(true)
                   where DerivesFrom(attribute.AttributeType, "System.Web.Mvc.ActionMethodSelectorAttribute")
                   select attribute;
        }

        private static bool DerivesFrom(IDeclaredType type, string superClassName)
        {
            return type.GetSuperTypes().Any(superType => superType.GetClrName().FullName == superClassName);
        }
    }
}