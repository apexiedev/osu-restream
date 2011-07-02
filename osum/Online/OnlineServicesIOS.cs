using System;
using MonoTouch.GameKit;
using MonoTouch.Foundation;
using osum.Support.iPhone;
using osum.Helpers;

namespace osum.Online
{
    public class OnlineServicesIOS : GKLeaderboardViewControllerDelegate, IOnlineServices
    {
        public OnlineServicesIOS()
        {
            localPlayer = GKLocalPlayer.LocalPlayer;
        }

        GKLocalPlayer localPlayer;

        static bool authSucceededThisSession;

        public void Authenticate()
        {
            if (!localPlayer.Authenticated)
                localPlayer.Authenticate(authenticationComplete);
        }

        void authenticationComplete(NSError error)
        {
            authSucceededThisSession |= error == null;
        }

        public bool IsAuthenticated {
            get { return localPlayer.Authenticated; }
        }

        void TriggerFinished()
        {
            VoidDelegate finished = finishedDelegate;
            if (finished != null)
            {
                finishedDelegate = null;
                finished();
            }
        }

        static VoidDelegate finishedDelegate;

        /// <summary>
        /// Shows the leaderboard.
        /// </summary>
        /// <param name='category'>
        /// The ID of the leaderboard. If null, it will display the aggregate leaderboard (see http://developer.apple.com/library/ios/#documentation/GameKit/Reference/GKLeaderboardViewController_Ref/Reference/Reference.html)
        /// </param>
        public void ShowLeaderboard(string category = null, VoidDelegate finished = null)
        {
            AppDelegate.UsingViewController = true;
            GKLeaderboardViewController vc = new GKLeaderboardViewController();
            vc.Category = category;
            vc.Delegate = this;
            finishedDelegate = finished;
            AppDelegate.ViewController.PresentModalViewController(vc, true);
        }

        public override void DidFinish(GKLeaderboardViewController viewController)
        {
            TriggerFinished();

            AppDelegate.ViewController.DismissModalViewControllerAnimated(false);
            //if we want to animate, we need to delay the removal of the view, else it gets stuck.
            //for now let's just not animate!

            AppDelegate.UsingViewController = false;
        }

        public void SubmitScore(string id, int score, VoidDelegate finished = null)
        {
            finishedDelegate = finished;
            GKScore gamekitScore = new GKScore(id);
            gamekitScore.Value = score;
            gamekitScore.ReportScore(delegate(NSError error) {
                if (error != null)
                {
#if DEBUG
                    //todo: handle this
                    Console.WriteLine("submission error: " + error.ToString());
                    Console.WriteLine("using id " + id + " score " + score);
#endif
                    finishedDelegate = null;
                    return;
                }

                TriggerFinished();
            });
        }
    }
}

