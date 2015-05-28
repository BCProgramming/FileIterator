using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BASeCamp.Search;

namespace BCFileSearch
{
    public class AsyncFileFindCompleteEventArgs : EventArgs
    {
        public enum CompletionCauseEnum
        {
            CompleteSuccess,
            CompleteCancelled,
            CompleteError
        }

        private CompletionCauseEnum _completionCause = CompletionCauseEnum.CompleteSuccess;

        public CompletionCauseEnum CompletionCause
        {
            get { return _completionCause; }
            set { _completionCause = value; }
        }

        public AsyncFileFindCompleteEventArgs(CompletionCauseEnum CompletionType)
        {
            _completionCause = CompletionType;
        }

        //fired when a FileFind operation completes.
    }

    public class AsyncFileFoundEventArgs : EventArgs
    {
        private FileSearchResult _Result = null;

        public FileSearchResult Result
        {
            get { return _Result; }
        }

        public AsyncFileFoundEventArgs(FileSearchResult result)
        {
            _Result = result;
        }
    }

    public class SearchErrorEventArgs : EventArgs
    {
        public Exception ExceptionCause { get; set; } = null;

        public SearchErrorEventArgs(Exception ErrorCause)
        {
            ExceptionCause = ErrorCause;
        }
    }
    public class SearchAlreadyInProgressException : Exception
    {
        public SearchAlreadyInProgressException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public SearchAlreadyInProgressException() : base("AsyncFileFinder Search Already started")
        {
        }
    }

    public class AsyncFileFinder
    {
        public delegate bool FilterDelegate(FileSearchResult fsearch);

        private bool _Cancelled = false;
        private bool isChild = false;
        private String _SearchDirectory = "";
        private String _SearchMask = "*";


        private FilterDelegate FileFilter = null;
        private FilterDelegate DirectoryRecursionFilter = null;
        private Thread SearchThread = null;
        private bool _IsSearching = false;
        private ConcurrentQueue<FileSearchResult> FoundElements = new ConcurrentQueue<FileSearchResult>();

        public event EventHandler<AsyncFileFindCompleteEventArgs> AsyncFileFindComplete;
        public event EventHandler<AsyncFileFoundEventArgs> AsyncFileFound;
        public event EventHandler<SearchErrorEventArgs> AsyncSearchError;
        public String SearchDirectory
        {
            get { return _SearchDirectory; }
            set { _SearchDirectory = value; }
        }

        public String SearchMask
        {
            get { return _SearchMask; }
            set { _SearchMask = value; }
        }

        public void Cancel()
        {
            lock (ChildDirectorySearchers)
            {
                foreach (var iterate in ChildDirectorySearchers)
                {
                    iterate.Cancel();
                }
            }
            _IsSearching = false;
            _Cancelled = true;
        }

        private void FireAsyncSearchError(Exception source)
        {
            SearchErrorEventArgs serror = new SearchErrorEventArgs(source);
            var copied = AsyncSearchError;
            copied?.Invoke(this, serror);
        }
        private void FireAsyncFileFound(AsyncFileFoundEventArgs e)
        {
            lock (this)
            {
                var copied = AsyncFileFound;
                copied?.Invoke(this, e);
            }
        }

        private void FireAsyncFileFindComplete(AsyncFileFindCompleteEventArgs.CompletionCauseEnum completionCauseEnum)
        {
            var copied = AsyncFileFindComplete;
            copied?.Invoke(this, new AsyncFileFindCompleteEventArgs(completionCauseEnum));
        }

        public void FireAsyncFileFindComplete()
        {
            FireAsyncFileFindComplete(AsyncFileFindCompleteEventArgs.CompletionCauseEnum.CompleteSuccess);
        }

        public bool IsSearching => _IsSearching;

        public bool HasResults()
        {
            return !FoundElements.IsEmpty;
        }

        public FileSearchResult GetNextResult()
        {
            FileSearchResult ResultItem = null;
            while (!FoundElements.TryDequeue(out ResultItem))
            {
            }
            return ResultItem;
        }

        public AsyncFileFinder(String pSearchDirectory, String pSearchMask, FilterDelegate pFileFilter = null,
            FilterDelegate pDirectoryRecursionFilter = null,
            bool pIsChild = false)
        {
            _SearchDirectory = pSearchDirectory;
            _SearchMask = pSearchMask;
            FileFilter = pFileFilter;
            DirectoryRecursionFilter = pDirectoryRecursionFilter;
            isChild = pIsChild;
        }
 
        public void Start()
        {
            //let's default our Filters if they aren't provided.
            if (FileFilter == null) FileFilter = ((s) => true);
            if (DirectoryRecursionFilter == null) DirectoryRecursionFilter = ((s) => true);
            if (IsSearching) throw new SearchAlreadyInProgressException();
            //alright, we want to do this asynchronously, so start StartInternal on another thread.
            _IsSearching = true;
            SearchThread = new Thread(StartSync);
            SearchThread.Start();
        }

        private bool FitsMask(string fileName, string fileMask)
        {
            return fileName.Like(fileMask);
        }

        private List<AsyncFileFinder> ChildDirectorySearchers = new List<AsyncFileFinder>();
        private int MaxChildren = 2;

        public void StartSync()
        {
            Exception searchError = null;
            try
            {
                lock (ChildDirectorySearchers)
                {
                    ChildDirectorySearchers = new List<AsyncFileFinder>();
                }
                Debug.Print("StartSync Called, Searching in " + _SearchDirectory + " For Mask " + _SearchMask);
                String sSearch = Path.Combine(_SearchDirectory, "*");
                Queue<String> directories = new Queue<string>();
                //Task: 
                //First, Search our folder for matching files and add them to the queue of results.
                Debug.Print("Searching for files in folder");
                NativeMethods.WIN32_FIND_DATA findData;
                IntPtr fHandle = NativeMethods.FindFirstFile(sSearch, out findData);
                while (fHandle != IntPtr.Zero)
                {
                    if (_Cancelled)
                    {
                        FireAsyncFileFindComplete(AsyncFileFindCompleteEventArgs.CompletionCauseEnum.CompleteCancelled);
                        return;
                    }
                    //if the result is a Directory, add it to the list of result directories if it passes the recursion test.
                    if ((findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        if (findData.Filename != "." && findData.Filename != "..")
                            if (
                                DirectoryRecursionFilter(new FileSearchResult(findData,
                                    Path.Combine(sSearch, findData.Filename))))
                            {
                                Debug.Print("Found Directory:" + findData.Filename + " Adding to Directory Queue.");
                                directories.Enqueue(findData.Filename);
                            }
                    }
                    else if (findData.Filename.Length > 0)
                    {
                        //make sure it matches the given mask.
                        if (FitsMask(findData.Filename, _SearchMask))
                        {
                            FileSearchResult fsr = new FileSearchResult(findData,
                                Path.Combine(_SearchDirectory, findData.Filename));
                            if (FileFilter(fsr) && !_Cancelled)
                            {
                                Debug.Print("Found File " + fsr.FullPath + " Raising Found event.");
                                FireAsyncFileFound(new AsyncFileFoundEventArgs(fsr));
                            }
                        }
                    }
                    findData = new NativeMethods.WIN32_FIND_DATA();
                    if (!NativeMethods.FindNextFile(fHandle, out findData))
                    {
                        Debug.Print("FindNextFile returned False, closing handle...");
                        NativeMethods.FindClose(fHandle);
                        fHandle = IntPtr.Zero;
                    }
                }


                //find all directories in the search folder which also satisfy the Recursion test.
                //Construct a new AsyncFileFinder to search within that folder with the same Mask and delegates for each one.
                //Allow MaxChildren to run at once. When a running filefinder raises it's complete event, remove it from the List, and start up one of the ones that have not been run.
                //if isChild is true, we won't actually multithread this task at all.

                Debug.Print("File Search completed. Starting search of " + directories.Count +
                            " directories found in folder " + _SearchDirectory);
                while (directories.Count > 0 || ChildDirectorySearchers.Count > 0)
                {
                    if (_Cancelled)
                    {
                        break;
                    }
                    while (ChildDirectorySearchers.Count >= MaxChildren) Thread.Sleep(5);
                    //add enough AsyncFileFinders to the ChildDirectorySearchers bag to hit the MaxChildren limit.

                    if (directories.Count == 0)
                    {
                        Debug.Print("No directories left. Waiting for Child Search instances to complete.");
                        Thread.Sleep(5);
                        continue;
                    }
                    Debug.Print("There are " + ChildDirectorySearchers.Count + " Searchers active. Starting more.");
                    String startchilddir = directories.Dequeue();
                    startchilddir = Path.Combine(_SearchDirectory, startchilddir);
                    AsyncFileFinder ChildSearcher = new AsyncFileFinder(startchilddir, _SearchMask, FileFilter,
                        DirectoryRecursionFilter, true);
                    ChildSearcher.AsyncFileFound += (senderchild, foundevent) =>
                    {
                        AsyncFileFinder source = senderchild as AsyncFileFinder;

                        if (!_Cancelled) FireAsyncFileFound(foundevent);
                    };
                    ChildSearcher.AsyncFileFindComplete += (ob, ev) =>
                    {
                        AsyncFileFinder ChildSearch = (AsyncFileFinder) ob;
                        lock (ChildDirectorySearchers)
                        {
                            Debug.Print("Child Searcher " + ChildSearch.SearchDirectory +
                                        " issued a completion event, removing from list.");
                            ChildDirectorySearchers.Remove(ChildSearch);
                        }
                    };

                    lock (ChildDirectorySearchers)
                    {
                        ChildDirectorySearchers.Add(ChildSearcher);
                    }

                    if (!isChild)
                    {
                        Debug.Print("Starting sub-search asynchronously");
                        ChildSearcher.Start();
                    }
                    else
                    {
                        Debug.Print("Starting sub-search synchronously");
                        ChildSearcher.StartSync();
                    }
                }
                Debug.Print("Exited Main Search Loop: Queue:" + directories.Count + " Child Searchers:" +
                            ChildDirectorySearchers.Count);
            }
            catch (Exception exx)
            {
                searchError = exx;
            }

            _IsSearching = false;
            if (searchError != null)
            {
                FireAsyncSearchError(searchError);
            }
            var completecause = _Cancelled
                ? AsyncFileFindCompleteEventArgs.CompletionCauseEnum.CompleteCancelled
                : AsyncFileFindCompleteEventArgs.CompletionCauseEnum.CompleteSuccess;
            if (searchError != null) completecause = AsyncFileFindCompleteEventArgs.CompletionCauseEnum.CompleteError; 
            FireAsyncFileFindComplete
                (completecause);
        }
    }
}