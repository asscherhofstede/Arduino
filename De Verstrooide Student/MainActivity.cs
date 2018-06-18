﻿using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using Android.Gms.Common;
using Firebase.Messaging;
using Firebase.Iid;
using Android.Util;
using System;
using Android.Content;

namespace De_Verstrooide_Student
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        TextView msgText;
        const string TAG = "MainActivity";
        Intent intent;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            msgText = FindViewById<TextView>(Resource.Id.msgText);

            if (Intent.Extras != null)
            {
                foreach (var key in Intent.Extras.KeySet())
                {
                    var value = Intent.Extras.GetString(key);
                    Log.Debug(TAG, "Key: {0} Value: {1}", key, value);
                }
            }

            IsPlayServicesAvailable();

            var logTokenButton = FindViewById<Button>(Resource.Id.logTokenButton);
            logTokenButton.Click += delegate
            {
                Log.Debug(TAG, "InstanceID token: {0}", FirebaseInstanceId.Instance.Token);
            };

            var subscribeButton = FindViewById<Button>(Resource.Id.subscribeButton);
            subscribeButton.Click += delegate
            {
                FirebaseMessaging.Instance.SubscribeToTopic("news");
                Log.Debug(TAG, "Subscribed to remote notifications");
            };
        }

        bool IsPlayServicesAvailable()
        {
            int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
            if(resultCode != ConnectionResult.Success)
            {
                if (GoogleApiAvailability.Instance.IsUserResolvableError(resultCode))
                {
                    msgText.Text = GoogleApiAvailability.Instance.GetErrorString(resultCode);
                }
                else
                {
                    msgText.Text = "This device is not supported";
                    Finish();
                }
                return false;
            }
            else
            {
                msgText.Text = "Google Play Services is available";
                return true;
            }
        }
    }
}

