// Papercut
// 
// Copyright � 2008 - 2012 Ken Robertson
// Copyright � 2013 - 2017 Jaben Cargman
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//  
// http://www.apache.org/licenses/LICENSE-2.0
//  
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License. 

namespace Papercut.Rules.Implementations
{
    using System;

    using MimeKit;

    using Papercut.Message;

    using Serilog;

    public class ConditionalForwardRuleDispatch : BaseRelayRuleDispatch<ConditionalForwardRule>
    {
        public ConditionalForwardRuleDispatch(Lazy<MimeMessageLoader> mimeMessageLoader, ILogger logger)
            : base(mimeMessageLoader, logger)
        {
        }

        protected override void PopulateMessageFromRule(ConditionalForwardRule rule, MimeMessage mimeMessage)
        {
            mimeMessage.PopulateFromRule(rule);
            base.PopulateMessageFromRule(rule, mimeMessage);
        }

        protected override bool RuleMatches(ConditionalForwardRule rule, MimeMessage mimeMessage)
        {
            return rule.IsConditionalForwardRuleMatch(mimeMessage);
        }
    }
}