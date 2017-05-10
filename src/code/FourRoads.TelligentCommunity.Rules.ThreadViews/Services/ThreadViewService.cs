using System;
using System.Linq;
using FourRoads.TelligentCommunity.Rules.ThreadViews.Events;
using FourRoads.TelligentCommunity.Rules.ThreadViews.Interfaces;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace FourRoads.TelligentCommunity.Rules.ThreadViews.Services
{
    public class ThreadViewService : IThreadViewService
    {
        private readonly IThreadViewDataProvider _forumThreadViewDataProvider;
        private readonly IUsers _users;
        private readonly IThreadViewEvents _events;

        public IThreadViewEvents Events => _events ?? new ThreadViewEvents();

        public ThreadViewService(IThreadViewDataProvider forumThreadViewDataProvider, IUsers users , IThreadViewEvents events = null)
        {
            _forumThreadViewDataProvider = forumThreadViewDataProvider;
            _users = users;
            _events = events;
        }

        public bool Create(Guid contentid, DateTime created)
        {
            _forumThreadViewDataProvider.Create(_users.ApplicationTypeId , contentid , (int)_users.AccessingUser.Id, created);
            return true;
        }

        public bool CheckforViewTriggers(int threshold)
        {
            var views = _forumThreadViewDataProvider.GetNewList(threshold);
            if (views.Any())
            {
                foreach (var view in views)
                {
                    _events.AfterView(view.ContentId);
                }
            }

            return true;
        }

    }
}
