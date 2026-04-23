using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace HELIOS.Platform.Operations
{
    /// <summary>
    /// Support ticket system with knowledge base, SLA tracking, and multi-channel support
    /// </summary>
    public class CustomerSupport
    {
        public enum TicketStatus { Open, InProgress, Pending, Resolved, Closed }
        public enum TicketPriority { Low, Medium, High, Critical }
        public enum TicketChannel { Email, Chat, Phone, SelfService }

        public class SupportTicket
        {
            public string TicketId { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public TicketStatus Status { get; set; }
            public TicketPriority Priority { get; set; }
            public TicketChannel Channel { get; set; }
            public string CustomerId { get; set; }
            public string CustomerName { get; set; }
            public string AssignedTo { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? AssignedAt { get; set; }
            public DateTime? ResolvedAt { get; set; }
            public DateTime? ClosedAt { get; set; }
            public List<TicketComment> Comments { get; set; }
            public List<string> Tags { get; set; }
            public string Category { get; set; }
        }

        public class TicketComment
        {
            public string CommentId { get; set; }
            public string TicketId { get; set; }
            public string Author { get; set; }
            public string Text { get; set; }
            public DateTime CreatedAt { get; set; }
            public bool IsInternal { get; set; }
            public List<string> Attachments { get; set; }
        }

        public class KnowledgeBaseArticle
        {
            public string ArticleId { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
            public List<string> Keywords { get; set; }
            public string Category { get; set; }
            public int ViewCount { get; set; }
            public int HelpfulCount { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public string Author { get; set; }
        }

        public class SupportBot
        {
            public string BotId { get; set; }
            public string Name { get; set; }
            public List<BotResponse> ResponseRules { get; set; }
            public int ResolutionRate { get; set; }
            public bool Enabled { get; set; }
        }

        public class BotResponse
        {
            public string RuleId { get; set; }
            public List<string> Triggers { get; set; }
            public string Response { get; set; }
            public List<string> SuggestedArticles { get; set; }
            public bool RequiresHumanReview { get; set; }
        }

        public class SLATracker
        {
            public string TrackerId { get; set; }
            public string TicketId { get; set; }
            public TicketPriority Priority { get; set; }
            public TimeSpan ResponseTime { get; set; }
            public TimeSpan ResolutionTime { get; set; }
            public DateTime? FirstResponseAt { get; set; }
            public DateTime? SLABreach { get; set; }
            public bool MetSLA { get; set; }
        }

        public class MultiChannelConversation
        {
            public string ConversationId { get; set; }
            public string CustomerId { get; set; }
            public Dictionary<TicketChannel, ConversationThread> Threads { get; set; }
            public DateTime StartedAt { get; set; }
            public DateTime? EndedAt { get; set; }
            public string Context { get; set; }
        }

        public class ConversationThread
        {
            public TicketChannel Channel { get; set; }
            public List<ChannelMessage> Messages { get; set; }
            public string CurrentAgent { get; set; }
        }

        public class ChannelMessage
        {
            public string MessageId { get; set; }
            public string From { get; set; }
            public string To { get; set; }
            public string Content { get; set; }
            public DateTime Timestamp { get; set; }
            public MessageStatus Status { get; set; }
        }

        public enum MessageStatus { Sent, Delivered, Read, Failed }

        private readonly Dictionary<string, SupportTicket> _tickets = new();
        private readonly List<KnowledgeBaseArticle> _kbArticles = new();
        private readonly Dictionary<string, SupportBot> _bots = new();
        private readonly Dictionary<string, SLATracker> _slaTrackers = new();
        private readonly Dictionary<string, MultiChannelConversation> _conversations = new();
        private readonly Dictionary<string, SupportTeamMember> _teamMembers = new();

        public class SupportTeamMember
        {
            public string MemberId { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public List<string> Specializations { get; set; }
            public int ActiveTickets { get; set; }
            public decimal AverageResolutionTime { get; set; }
            public decimal CustomerSatisfactionScore { get; set; }
        }

        public CustomerSupport()
        {
            InitializeBot();
            InitializeKnowledgeBase();
            InitializeTeam();
        }

        public SupportTicket CreateTicket(string title, string description, TicketPriority priority,
            TicketChannel channel, string customerId, string customerName, string category)
        {
            var ticket = new SupportTicket
            {
                TicketId = Guid.NewGuid().ToString(),
                Title = title,
                Description = description,
                Status = TicketStatus.Open,
                Priority = priority,
                Channel = channel,
                CustomerId = customerId,
                CustomerName = customerName,
                CreatedAt = DateTime.UtcNow,
                Comments = new List<TicketComment>(),
                Tags = new List<string>(),
                Category = category
            };

            _tickets[ticket.TicketId] = ticket;

            var slaTracker = new SLATracker
            {
                TrackerId = Guid.NewGuid().ToString(),
                TicketId = ticket.TicketId,
                Priority = priority,
                ResponseTime = GetSLAResponseTime(priority),
                ResolutionTime = GetSLAResolutionTime(priority),
                MetSLA = true
            };

            _slaTrackers[ticket.TicketId] = slaTracker;
            return ticket;
        }

        public async Task<bool> AssignTicket(string ticketId, string supportAgentId)
        {
            if (!_tickets.TryGetValue(ticketId, out var ticket))
                return false;

            ticket.AssignedTo = supportAgentId;
            ticket.AssignedAt = DateTime.UtcNow;
            ticket.Status = TicketStatus.InProgress;

            if (_teamMembers.TryGetValue(supportAgentId, out var member))
            {
                member.ActiveTickets++;
            }

            if (_slaTrackers.TryGetValue(ticketId, out var slaTracker))
            {
                slaTracker.FirstResponseAt = DateTime.UtcNow;
            }

            await Task.CompletedTask;
            return true;
        }

        public async Task<TicketComment> AddCommentToTicket(string ticketId, string author, string text,
            bool isInternal = false, List<string> attachments = null)
        {
            if (!_tickets.TryGetValue(ticketId, out var ticket))
                return null;

            var comment = new TicketComment
            {
                CommentId = Guid.NewGuid().ToString(),
                TicketId = ticketId,
                Author = author,
                Text = text,
                CreatedAt = DateTime.UtcNow,
                IsInternal = isInternal,
                Attachments = attachments ?? new List<string>()
            };

            ticket.Comments.Add(comment);
            await Task.CompletedTask;
            return comment;
        }

        public async Task<bool> ResolveTicket(string ticketId, string resolution)
        {
            if (!_tickets.TryGetValue(ticketId, out var ticket))
                return false;

            ticket.Status = TicketStatus.Resolved;
            ticket.ResolvedAt = DateTime.UtcNow;

            await AddCommentToTicket(ticketId, "System", resolution, false);

            if (_slaTrackers.TryGetValue(ticketId, out var slaTracker))
            {
                var actualTime = ticket.ResolvedAt.Value - ticket.CreatedAt;
                slaTracker.MetSLA = actualTime <= slaTracker.ResolutionTime;
                if (!slaTracker.MetSLA)
                {
                    slaTracker.SLABreach = ticket.ResolvedAt;
                }
            }

            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> CloseTicket(string ticketId)
        {
            if (!_tickets.TryGetValue(ticketId, out var ticket))
                return false;

            ticket.Status = TicketStatus.Closed;
            ticket.ClosedAt = DateTime.UtcNow;

            if (ticket.AssignedTo != null && _teamMembers.TryGetValue(ticket.AssignedTo, out var member))
            {
                member.ActiveTickets = Math.Max(0, member.ActiveTickets - 1);
            }

            await Task.CompletedTask;
            return true;
        }

        public async Task<List<KnowledgeBaseArticle>> SearchKnowledgeBase(string query, int limit = 10)
        {
            var results = _kbArticles
                .Where(a => a.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           a.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           a.Keywords.Any(k => k.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(a => a.ViewCount)
                .Take(limit)
                .ToList();

            foreach (var article in results)
            {
                article.ViewCount++;
            }

            await Task.CompletedTask;
            return results;
        }

        public async Task<List<KnowledgeBaseArticle>> GetArticlesByCategory(string category, int limit = 20)
        {
            var results = _kbArticles
                .Where(a => a.Category == category)
                .OrderByDescending(a => a.ViewCount)
                .Take(limit)
                .ToList();

            await Task.CompletedTask;
            return results;
        }

        public void CreateKnowledgeBaseArticle(string title, string content, List<string> keywords, string category)
        {
            var article = new KnowledgeBaseArticle
            {
                ArticleId = Guid.NewGuid().ToString(),
                Title = title,
                Content = content,
                Keywords = keywords,
                Category = category,
                ViewCount = 0,
                HelpfulCount = 0,
                CreatedAt = DateTime.UtcNow,
                Author = "SupportTeam"
            };

            _kbArticles.Add(article);
        }

        public async Task<(List<KnowledgeBaseArticle>, string)> GetChatbotResponse(string userMessage)
        {
            var bot = _bots.Values.FirstOrDefault(b => b.Enabled);
            if (bot == null)
                return (new List<KnowledgeBaseArticle>(), "I'm unable to assist at the moment.");

            foreach (var rule in bot.ResponseRules)
            {
                if (rule.Triggers.Any(t => userMessage.Contains(t, StringComparison.OrdinalIgnoreCase)))
                {
                    var articles = rule.SuggestedArticles.Count > 0 ?
                        _kbArticles.Where(a => rule.SuggestedArticles.Contains(a.ArticleId)).ToList() :
                        new List<KnowledgeBaseArticle>();

                    await Task.CompletedTask;
                    return (articles, rule.Response);
                }
            }

            await Task.CompletedTask;
            return (new List<KnowledgeBaseArticle>(), "Could you provide more details about your issue?");
        }

        public async Task<MultiChannelConversation> StartMultiChannelConversation(string customerId)
        {
            var conversation = new MultiChannelConversation
            {
                ConversationId = Guid.NewGuid().ToString(),
                CustomerId = customerId,
                Threads = new Dictionary<TicketChannel, ConversationThread>(),
                StartedAt = DateTime.UtcNow
            };

            foreach (var channel in Enum.GetValues(typeof(TicketChannel)).Cast<TicketChannel>())
            {
                conversation.Threads[channel] = new ConversationThread
                {
                    Channel = channel,
                    Messages = new List<ChannelMessage>()
                };
            }

            _conversations[conversation.ConversationId] = conversation;
            await Task.CompletedTask;
            return conversation;
        }

        public async Task<ChannelMessage> AddChannelMessage(string conversationId, TicketChannel channel,
            string from, string to, string content)
        {
            if (!_conversations.TryGetValue(conversationId, out var conversation))
                return null;

            if (!conversation.Threads.TryGetValue(channel, out var thread))
                return null;

            var message = new ChannelMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                From = from,
                To = to,
                Content = content,
                Timestamp = DateTime.UtcNow,
                Status = MessageStatus.Sent
            };

            thread.Messages.Add(message);
            await Task.Delay(50);
            message.Status = MessageStatus.Delivered;

            return message;
        }

        public async Task<SupportMetricsReport> GenerateSupportMetricsReport(DateTime startDate, DateTime endDate)
        {
            var ticketsInPeriod = _tickets.Values
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .ToList();

            var report = new SupportMetricsReport
            {
                ReportId = Guid.NewGuid().ToString(),
                GeneratedAt = DateTime.UtcNow,
                StartDate = startDate,
                EndDate = endDate,
                TotalTickets = ticketsInPeriod.Count,
                ResolvedTickets = ticketsInPeriod.Count(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed),
                AverageResolutionTime = CalculateAverageResolutionTime(ticketsInPeriod),
                AverageFirstResponseTime = CalculateAverageFirstResponseTime(ticketsInPeriod),
                SLAComplianceRate = CalculateSLACompliance(),
                TicketsByPriority = ticketsInPeriod.GroupBy(t => t.Priority)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                TicketsByChannel = ticketsInPeriod.GroupBy(t => t.Channel)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                TopCategories = ticketsInPeriod.GroupBy(t => t.Category)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToList(),
                TeamPerformance = _teamMembers.Values
                    .Select(m => new { Member = m.Name, Satisfaction = m.CustomerSatisfactionScore, Active = m.ActiveTickets })
                    .ToList()
            };

            await Task.CompletedTask;
            return report;
        }

        public class SupportMetricsReport
        {
            public string ReportId { get; set; }
            public DateTime GeneratedAt { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int TotalTickets { get; set; }
            public int ResolvedTickets { get; set; }
            public TimeSpan AverageResolutionTime { get; set; }
            public TimeSpan AverageFirstResponseTime { get; set; }
            public decimal SLAComplianceRate { get; set; }
            public Dictionary<string, int> TicketsByPriority { get; set; }
            public Dictionary<string, int> TicketsByChannel { get; set; }
            public List<object> TopCategories { get; set; }
            public List<object> TeamPerformance { get; set; }
        }

        private TimeSpan GetSLAResponseTime(TicketPriority priority)
        {
            return priority switch
            {
                TicketPriority.Critical => TimeSpan.FromMinutes(15),
                TicketPriority.High => TimeSpan.FromHours(1),
                TicketPriority.Medium => TimeSpan.FromHours(4),
                _ => TimeSpan.FromHours(8)
            };
        }

        private TimeSpan GetSLAResolutionTime(TicketPriority priority)
        {
            return priority switch
            {
                TicketPriority.Critical => TimeSpan.FromHours(2),
                TicketPriority.High => TimeSpan.FromHours(8),
                TicketPriority.Medium => TimeSpan.FromDays(1),
                _ => TimeSpan.FromDays(3)
            };
        }

        private TimeSpan CalculateAverageResolutionTime(List<SupportTicket> tickets)
        {
            var resolvedTickets = tickets.Where(t => t.ResolvedAt.HasValue).ToList();
            if (resolvedTickets.Count == 0)
                return TimeSpan.Zero;

            var totalTime = resolvedTickets.Aggregate(TimeSpan.Zero,
                (acc, t) => acc.Add(t.ResolvedAt.Value - t.CreatedAt));

            return new TimeSpan(totalTime.Ticks / resolvedTickets.Count);
        }

        private TimeSpan CalculateAverageFirstResponseTime(List<SupportTicket> tickets)
        {
            var respondedTickets = tickets.Where(t => t.AssignedAt.HasValue).ToList();
            if (respondedTickets.Count == 0)
                return TimeSpan.Zero;

            var totalTime = respondedTickets.Aggregate(TimeSpan.Zero,
                (acc, t) => acc.Add(t.AssignedAt.Value - t.CreatedAt));

            return new TimeSpan(totalTime.Ticks / respondedTickets.Count);
        }

        private decimal CalculateSLACompliance()
        {
            if (_slaTrackers.Count == 0)
                return 100;

            var compliant = _slaTrackers.Values.Count(s => s.MetSLA);
            return (compliant * 100m) / _slaTrackers.Count;
        }

        private void InitializeBot()
        {
            var bot = new SupportBot
            {
                BotId = Guid.NewGuid().ToString(),
                Name = "HeliosBot",
                ResponseRules = new List<BotResponse>
                {
                    new BotResponse
                    {
                        RuleId = "rule1",
                        Triggers = new List<string> { "password", "login", "reset" },
                        Response = "I can help with password resets. Please visit the following article:",
                        SuggestedArticles = new List<string>(),
                        RequiresHumanReview = false
                    },
                    new BotResponse
                    {
                        RuleId = "rule2",
                        Triggers = new List<string> { "billing", "invoice", "payment" },
                        Response = "For billing inquiries, let me connect you with our billing team.",
                        SuggestedArticles = new List<string>(),
                        RequiresHumanReview = true
                    }
                },
                ResolutionRate = 40,
                Enabled = true
            };

            _bots[bot.BotId] = bot;
        }

        private void InitializeKnowledgeBase()
        {
            CreateKnowledgeBaseArticle("Getting Started", "Quick start guide for new users",
                new List<string> { "start", "guide", "new" }, "General");
            CreateKnowledgeBaseArticle("Password Reset", "Steps to reset your password",
                new List<string> { "password", "reset", "login" }, "Account");
            CreateKnowledgeBaseArticle("Billing FAQ", "Frequently asked questions about billing",
                new List<string> { "billing", "invoice", "payment" }, "Billing");
        }

        private void InitializeTeam()
        {
            var members = new[]
            {
                new SupportTeamMember { MemberId = "agent1", Name = "Alice Support", Email = "alice@support.com",
                    Specializations = new List<string> { "Technical", "Billing" }, AverageResolutionTime = 120, CustomerSatisfactionScore = 4.8m },
                new SupportTeamMember { MemberId = "agent2", Name = "Bob Support", Email = "bob@support.com",
                    Specializations = new List<string> { "Technical", "Account" }, AverageResolutionTime = 150, CustomerSatisfactionScore = 4.6m }
            };

            foreach (var member in members)
            {
                _teamMembers[member.MemberId] = member;
            }
        }
    }
}
