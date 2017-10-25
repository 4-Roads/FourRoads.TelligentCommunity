using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensions.Calendar.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensions.Calendar.Extensibility.Api.Version1;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions.Enumerations
{
    public class CalendarEnumerate : GenericEnumerate<Calendar>
    {
        public int _groupId;
        public int? _calendarId;

        public CalendarEnumerate(int groupId, int? calendarId)
        {
            _groupId = groupId;
            _calendarId = calendarId;
        }

        protected override PagedList<Calendar> NextPage(int pageIndex)
        {
            if (_calendarId == null)
            {
                //This works around the issue of not being able to filter by goup id, not ideal and not very fast
                while (true)
                {
                    var result = Apis.Get<ICalendars>().List(new CalendarsListOptions() {PageIndex = pageIndex, PageSize = 1});

                    if (result.Count == 0)
                        break;

                    if (result[0].Group.Id == _groupId)
                        return result;

                    pageIndex++;
                }
            }

            return new PagedList<Calendar>(new[] { Apis.Get<ICalendars>().Show(new CalendarsShowOptions() { Id = _calendarId.Value }) }, 1, 0, 1);
        }
    }
}