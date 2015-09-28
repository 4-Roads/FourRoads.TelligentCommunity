using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;

namespace FourRoads.TelligentCommunity.ConfigurationExtensions.Enumerations
{
    public abstract class GenericEnumerate<T> : IEnumerator<T> , IEnumerable<T>
    {
        private PagedList<T> _currentResults = null;
        private int _currentIndex = 0;
        private int _currentPage = 0;

        public void Dispose()
        {
            _currentResults = null;
        }

        protected abstract PagedList<T> NextPage(int pageIndex);

        public bool MoveNext()
        {
            _currentIndex++;

            if (_currentResults != null)
            {
                if (_currentIndex >= _currentResults.PageSize)
                {
                    _currentIndex = 0;
                    _currentPage++;

                    _currentResults = NextPage(_currentPage);
                }

                if ((_currentPage * _currentResults.PageSize) + _currentIndex >= _currentResults.TotalCount)
                    return false;
            }
            else
            {
                _currentIndex = 0;
                _currentPage = 0;
                _currentResults = NextPage(_currentPage);

                if (_currentResults.TotalCount == 0)
                    return false;
            }

            return true;
        }

        public void Reset()
        {
            _currentResults = null;
        }

        public T Current 
        {
            get { return _currentResults.ElementAt(_currentIndex); }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }


        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}