using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using viafront3.Models;

namespace viafront3.Services
{
    public class UserLock
    {
        public DateTime Created;
        public DateTime LastAccessed;

        public UserLock()
        {
            Created = DateTime.Now;
            LastAccessed = DateTime.Now;
        }
    }

    public interface IUserLocks
    {
        UserLock GetLock(string userId);
    }

    public class UserLocks : IUserLocks
    {
        readonly Dictionary<string, WeakReference<UserLock>> userLocks = new Dictionary<string, WeakReference<UserLock>>();

        public UserLocks()
        {
        }

        public UserLock GetLock(string userId)
        {
            lock (userLocks)
            {
                UserLock ul = null;
                if (userLocks.ContainsKey(userId))
                {
                    // get the weak reference target, if it has been garbage collected, regenerate it
                    if (!userLocks[userId].TryGetTarget(out ul))
                    {
                        ul = new UserLock();
                        userLocks[userId].SetTarget(ul);
                    }
                }
                else
                {
                    ul = new UserLock();
                    userLocks[userId] = new WeakReference<UserLock>(ul);
                }
                ul.LastAccessed = DateTime.Now;
                return ul;
            }
        }
    }
}
