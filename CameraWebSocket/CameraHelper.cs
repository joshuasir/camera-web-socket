﻿using EOSDigital.API;
using EOSDigital.SDK;
using System.IO;

namespace CameraWebSocket
{
    public class CameraHelper
    {

        static CanonAPI APIHandler;
        static Camera MainCamera;
        static string ImageSaveDirectory;
        static bool Error = false;
        static ManualResetEvent WaitEvent = new ManualResetEvent(false);
        static private string FilePath = "";
        static public string SnapPhoto()
        {
            try
            {
                APIHandler = new CanonAPI();
                List<Camera> cameras = APIHandler.GetCameraList();
                if (!OpenSession())
                {
                    Console.WriteLine("No camera found. Please plug in camera");
                    APIHandler.CameraAdded += APIHandler_CameraAdded;
                    WaitEvent.WaitOne();
                    WaitEvent.Reset();
                }

                if (!Error)
                {
                    ImageSaveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "RemotePhoto");
                    MainCamera.SetSetting(PropertyID.SaveTo, (int)SaveTo.Host);
                    MainCamera.SetCapacity(4096, int.MaxValue);
                    Console.WriteLine($"Set image output path to: {ImageSaveDirectory}");

                    Console.WriteLine("Taking photo with current settings...");
                    CameraValue tv = TvValues.GetValue(MainCamera.GetInt32Setting(PropertyID.Tv));
                    if (tv == TvValues.Bulb) MainCamera.TakePhotoBulb(2);
                    else MainCamera.TakePhoto();
                    WaitEvent.WaitOne();
                    //MainCamera.DownloadFile(DownloadInfo, ImageSaveDirectory);
                    if (!Error)
                    {
                        Console.WriteLine("Photo taken and saved");
                        return FilePath;
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("Error: " + ex.Message); }
            finally
            {
                MainCamera?.Dispose();
                APIHandler.Dispose();
                Console.WriteLine("Good bye! (press any key to close)");
                Console.ReadKey();
            }

            return "";
        }

        private static void APIHandler_CameraAdded(CanonAPI sender)
        {
            try
            {
                Console.WriteLine("Camera added event received");
                if (!OpenSession()) { Console.WriteLine("Sorry, something went wrong. No camera"); Error = true; }
            }
            catch (Exception ex) { Console.WriteLine("Error: " + ex.Message); Error = true; }
            finally { WaitEvent.Set(); }
        }

        private static void MainCamera_DownloadReady(Camera sender, DownloadInfo Info)
        {
            try
            {
                Console.WriteLine("Starting image download...");
                sender.DownloadFile(Info, ImageSaveDirectory);
                FilePath = Path.Combine(ImageSaveDirectory, Info.FileName);
            }
            catch (Exception ex) { Console.WriteLine("Error: " + ex.Message); Error = true; }
            finally { WaitEvent.Set(); }
        }

        private static bool OpenSession()
        {
            List<Camera> cameras = APIHandler.GetCameraList();
            if (cameras.Count > 0)
            {
                MainCamera = cameras[0];
                MainCamera.DownloadReady += MainCamera_DownloadReady;
                MainCamera.OpenSession();
                Console.WriteLine($"Opened session with camera: {MainCamera.DeviceName}");
                return true;
            }
            else return false;
        }
    }
}
