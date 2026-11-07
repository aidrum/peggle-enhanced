using System;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.Xna.Framework.Input;
using Sexy;

namespace Peggle.Android
{
    // Declared in AndroidManifest.xml (not with [Activity]), under this Java name, so the launcher icon
    // aliases there can follow it; the label is the app's (ApplicationTitle).
    [Register("peggle.android.GameActivity")]
    public class GameActivity : Microsoft.Xna.Framework.AndroidGameActivity
    {
        private Sexy.Main mGame;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            // Let the game draw behind the camera cutout too; by default the window (and so the game's
            // surface) stops short of it, leaving a black band.
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.Always;
            }
            else if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }
            Window.Attributes = Window.Attributes;

            // The game data is read straight from the APK's assets; resolved paths are then the asset
            // paths ("Content/...").
            ContentFS.Root = ApkContentSource.AssetRoot;
            ContentFS.Source = new ApkContentSource(this);
            // Earlier builds copied the data into app storage on first launch; free that space.
            string oldContentDir = System.IO.Path.Combine(FilesDir.AbsolutePath, "Content");
            if (Directory.Exists(oldContentDir))
            {
                Task.Run(() =>
                {
                    try
                    {
                        Directory.Delete(oldContentDir, true);
                    }
                    catch (IOException)
                    {
                    }
                });
            }

            // Saves live in the app's data folder (Android/data/<package>/files), falling back to
            // internal storage if shared storage is unavailable.
            string saveDir = GetExternalFilesDir(null)?.AbsolutePath ?? FilesDir.AbsolutePath;
            Directory.CreateDirectory(saveDir);

            // Text fields open the system keyboard prompt, as iOS showed its keyboard.
            EditManager.TextPromptHandler = (theText, theDone) =>
                KeyboardInput.Show("Peggle", "", theText).ContinueWith(t => theDone(t.IsCompletedSuccessfully ? t.Result : null));

            ImageFileLoader.PlatformDecoder = DecodeImage;
            // StreamMusicInterface defaults to VorbisMusicPlayer (seamless NVorbis + DynamicSoundEffectInstance).
            Sexy.Main.NativeScreenSize = GetScreenSize();
            Sexy.Main.AppFactory = game => new ThunderballApp(game);
            // Quitting from the game closes the app for real (the game has already saved), so reopening
            // it starts fresh instead of resuming a game that immediately exits again.
            Sexy.Main.ExitHandler = () =>
            {
                // A pending icon change (App Icon page) is applied now, after the game has saved: switching the
                // launcher alias ends the task anyway, and the process must not live on into the next launch.
                ApplyPendingAppIcon();
                RunOnUiThread(() =>
                {
                    FinishAndRemoveTask();
                    global::Android.OS.Process.KillProcess(global::Android.OS.Process.MyPid());
                });
            };
            // Links (More Games) open in the browser or, for Google Play links, the Play Store app.
            Sexy.Main.OpenUrlHandler = theUrl => RunOnUiThread(() =>
            {
                try
                {
                    Intent intent = new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(theUrl));
                    intent.AddFlags(ActivityFlags.NewTask);
                    StartActivity(intent);
                }
                catch (ActivityNotFoundException)
                {
                    Toast.MakeText(this, "No app can open " + theUrl, ToastLength.Long).Show();
                }
            });
            // The App Icon page (Extra Options) switches the launcher between the manifest's icon aliases.
            Sexy.Main.SetAppIconHandler = SetAppIcon;
            Sexy.Main.GetAppIconHandler = GetAppIcon;
            // Port addition (launch target): an app that starts the game can say where it should open, with the
            // string extra "peggle_target" (ThunderballApp.sLaunchTarget). Without it nothing changes.
            ThunderballApp.sLaunchTarget = Intent?.GetStringExtra("peggle_target");
            mGame = new Sexy.Main();
            mGame.applicationStoragePath = saveDir;
            View view = (View)mGame.Services.GetService(typeof(View));
            SetContentView(view);
            HideSystemUi();
            // The back button goes to the game (pause menu / quit prompt) instead of closing the activity.
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(0, new BackCallback());
            }
            mGame.Run();
        }

#pragma warning disable CA1422, CS0672
        public override void OnBackPressed()
        {
            Sexy.Main.BackRequested = true;
        }
#pragma warning restore CA1422, CS0672

        private class BackCallback : Java.Lang.Object, global::Android.Window.IOnBackInvokedCallback
        {
            public void OnBackInvoked()
            {
                Sexy.Main.BackRequested = true;
            }
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            if (hasFocus)
            {
                HideSystemUi();
            }
        }

        /// <summary>
        /// Decodes an image file's bytes on the CPU with BitmapFactory (as MonoGame's Android FromStream
        /// does, but without uploading to the GPU and reading back). Pixels stay straight alpha, packed as
        /// SurfaceFormat.Color (ABGR in a uint).
        /// </summary>
        private static uint[] DecodeImage(byte[] theFileData, out int theWidth, out int theHeight)
        {
            theWidth = 0;
            theHeight = 0;
            BitmapFactory.Options options = new BitmapFactory.Options
            {
                InScaled = false,
                InDither = false,
                InPremultiplied = false,
                InPreferredConfig = Bitmap.Config.Argb8888
            };
            using (Bitmap bitmap = BitmapFactory.DecodeByteArray(theFileData, 0, theFileData.Length, options))
            {
                if (bitmap == null)
                {
                    return null;
                }
                theWidth = bitmap.Width;
                theHeight = bitmap.Height;
                int[] argb = new int[theWidth * theHeight];
                bitmap.GetPixels(argb, 0, theWidth, 0, 0, theWidth, theHeight);
                uint[] pixels = new uint[argb.Length];
                for (int i = 0; i < argb.Length; i++)
                {
                    uint p = (uint)argb[i];
                    // ARGB -> ABGR: swap red and blue.
                    pixels[i] = (p & 0xFF00FF00u) | ((p >> 16) & 0xFFu) | ((p & 0xFFu) << 16);
                }
                return pixels;
            }
        }

        private ComponentName GetIconComponent(string theAlias)
        {
            // The aliases are named relative to the package (".IconIOS"), which differs per edition.
            return new ComponentName(PackageName, PackageName + "." + theAlias);
        }

        private string mPendingIconAlias;

        private string[] mPendingIconAll;

        /// <summary>
        /// Switches the launcher to theAlias's entry. Android ends the task when the alias it was launched from is
        /// disabled, and a process left behind would start the next launch without sound and then crash, so the
        /// game quits first (saving, as QUIT does) and the switch happens in ExitHandler, before the process ends.
        /// </summary>
        private void SetAppIcon(string theAlias, string[] theAll)
        {
            mPendingIconAlias = theAlias;
            mPendingIconAll = theAll;
            if (GlobalStaticVars.gSexyAppBase != null)
            {
                GlobalStaticVars.gSexyAppBase.WantsToExit = true;
            }
        }

        /// <summary>
        /// Enables the pending alias's launcher entry and disables the other icon aliases. The launcher may take a
        /// moment to show the new icon, and home-screen shortcuts to the old one may be removed.
        /// </summary>
        private void ApplyPendingAppIcon()
        {
            string theAlias = mPendingIconAlias;
            if (theAlias == null)
            {
                return;
            }
            mPendingIconAlias = null;
            try
            {
                // The new entry first, so the app always has a launcher entry.
                PackageManager.SetComponentEnabledSetting(GetIconComponent(theAlias), ComponentEnabledState.Enabled, ComponentEnableOption.DontKillApp);
                foreach (string anAlias in mPendingIconAll)
                {
                    if (anAlias != theAlias)
                    {
                        PackageManager.SetComponentEnabledSetting(GetIconComponent(anAlias), ComponentEnabledState.Disabled, ComponentEnableOption.DontKillApp);
                    }
                }
            }
            catch (Java.Lang.Exception e)
            {
                global::Android.Util.Log.Warn("Peggle", "Could not switch the app icon: " + e.Message);
            }
        }

        /// <summary>The icon alias the launcher shows: one enabled explicitly, else the manifest's default (the first).</summary>
        private string GetAppIcon(string[] theAll)
        {
            foreach (string anAlias in theAll)
            {
                try
                {
                    if (PackageManager.GetComponentEnabledSetting(GetIconComponent(anAlias)) == ComponentEnabledState.Enabled)
                    {
                        return anAlias;
                    }
                }
                catch (Java.Lang.Exception)
                {
                }
            }
            return theAll.Length != 0 ? theAll[0] : null;
        }

        /// <summary>The whole display in landscape (the system bars are hidden while playing).</summary>
        private Microsoft.Xna.Framework.Point GetScreenSize()
        {
            int w, h;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                var bounds = WindowManager.MaximumWindowMetrics.Bounds;
                w = bounds.Width();
                h = bounds.Height();
            }
            else
            {
#pragma warning disable CA1422
                var metrics = new global::Android.Util.DisplayMetrics();
                WindowManager.DefaultDisplay.GetRealMetrics(metrics);
#pragma warning restore CA1422
                w = metrics.WidthPixels;
                h = metrics.HeightPixels;
            }
            return new Microsoft.Xna.Framework.Point(Math.Max(w, h), Math.Min(w, h));
        }

        private void HideSystemUi()
        {
            // The insets controller needs the window's DecorView; only call this after SetContentView.
            if (Window.DecorView == null)
            {
                return;
            }
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                Window.SetDecorFitsSystemWindows(false);
                IWindowInsetsController controller = Window.InsetsController;
                if (controller != null)
                {
                    controller.Hide(WindowInsets.Type.SystemBars());
                    controller.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
                }
            }
            else
            {
#pragma warning disable CA1422
                Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(SystemUiFlags.LayoutStable |
                    SystemUiFlags.LayoutHideNavigation | SystemUiFlags.LayoutFullscreen | SystemUiFlags.HideNavigation |
                    SystemUiFlags.Fullscreen | SystemUiFlags.ImmersiveSticky);
#pragma warning restore CA1422
            }
        }
    }
}
