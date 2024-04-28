using Firebase;

namespace Source.Scripts.AnalyticsFirebase.FirebaseInit
{
    public class FirebaseInit
    {
        public static void Init()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                var dependencyStatus = task.Result;

                if (dependencyStatus == DependencyStatus.Available)
                {
                    Firebase.Analytics.FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                }
                else
                {
                    Init();
                }
            });
        }
    }
}