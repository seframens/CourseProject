using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntiClient.Events
{
    public static class ProjectEvent
    {
        public static event Action<int>? ProjectUpdated;

        public static event Action? ProjectsUpdated;

        public static void RaiseProjectUpdated(int projectId)
        {
            ProjectUpdated?.Invoke(projectId);
        }

        public static void RaiseProjectsUpdated()
        {
            ProjectsUpdated?.Invoke();
        }
    }
}
