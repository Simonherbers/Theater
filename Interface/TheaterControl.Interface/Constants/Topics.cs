// <copyright company="ROSEN Swiss AG">
//  Copyright (c) ROSEN Swiss AG
//  This computer program includes confidential, proprietary
//  information and is a trade secret of ROSEN. All use,
//  disclosure, or reproduction is prohibited unless authorized in
//  writing by an officer of ROSEN. All Rights Reserved.
// </copyright>

namespace TheaterControl.Interface.Constants
{
    public class Topics
    {
        #region Fields

        public const string BASE_TOPIC = "/Theater/";

        public const string DEVICE_TOPIC = "/Theater/Device";

        public const string MUSIC_TOPIC = "/Theater/music";

        public const string SCENE_CONFIGURATION_TOPIC = "/Theater/Scene";

        public const string SCENE_CONTROL_TOPIC = "/Theater/Control";

        public const string SONG_CONTROL_TOPIC_FROM_UI = "/Theater/SongControlUI";

        public const string SONG_TOPIC = "/Theater/songs";

        public const string SELECTION_TOPIC = "/Theater/Selection";

        #endregion
    }
}