using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BCFileSearch
{
    public abstract class CancellableEvent : EventArgs, ICancellable
    {
        public bool Cancelled { get; set; }
    }
    public class FileSearchFilterEvent : CancellableEvent
    {
        private IEnumerable<ISearchFilter> _PositiveFilters;
        private IEnumerable<ISearchFilter> _NegativeFilters;
        private FileSearchResult _Result;
        
        public IEnumerable<ISearchFilter> getPositiveFilters() { return _PositiveFilters; }
        public IEnumerable<ISearchFilter> getNegativeFilters() { return _NegativeFilters; }
        public FileSearchResult Result { get { return _Result; } }
        

        protected FileSearchFilterEvent(
            FileSearchResult pResult,
            IEnumerable<ISearchFilter> pPositiveFilters, IEnumerable<ISearchFilter> pNegativeFilters)
        {
            _PositiveFilters = pPositiveFilters;
            _NegativeFilters = pNegativeFilters;
            _Result = pResult;

        }

    }



    class FileSearch
    {
        public event EventHandler<FileSearchFilterEvent> FileFound;
        public event EventHandler<FileSearchFilterEvent> DirectoryRecurse;

        private IEnumerable<ISearchFilter> _Filters = new List<ISearchFilter>();
        public IEnumerable<ISearchFilter> Filters { get { return _Filters; } private set { _Filters = value; } }

        public bool FireCancellableEvent<T>(EventHandler<T> eventobject, T e) where T : CancellableEvent
        {
            bool cancelledstatus = false;
            bool DoIgnoreCancelled = false;
            var copied = eventobject;
            if (copied == null) return false;
            var invokelist = copied.GetInvocationList();
            foreach (var iterate in invokelist)
            {

            }
            return false;
        }
    
    

        public bool FireFileFound(FileSearchFilterEvent fireEvent)
        {
            bool cancelledstatus = false;
            bool DoIgnoreCancelled = false;
            var copied = FileFound;
            if (copied == null) return false;
            var invokelist = copied.GetInvocationList();
            foreach(var iterate in invokelist){
                var customattributes = iterate.Method.GetCustomAttributes(typeof(EventHandlerAttribute), true);
                if (customattributes.Length != 0)
                {
                    foreach (var lookattribute in customattributes)
                    {
                        EventHandlerAttribute casted = lookattribute as EventHandlerAttribute;
                        if (casted != null)
                        {
                            DoIgnoreCancelled = casted.IgnoreCancelled;
                            if (DoIgnoreCancelled) break; //break out.

                        }
                    }

                }
                //we've dealt with all the attributes on this method.
                //if set to ignore cancellation, don't call this one.
                if (DoIgnoreCancelled && cancelledstatus)
                {

                }
                else
                {
                    //otherwise, call it...
                    fireEvent.Cancelled = false;
                    copied(this, fireEvent);
                    cancelledstatus = fireEvent.Cancelled; 

                }


            }


            return false;

        }


        public FileSearch(String[] SearchFolders,IEnumerable<ISearchFilter> SearchFilters)
        {

        }





    }
}
