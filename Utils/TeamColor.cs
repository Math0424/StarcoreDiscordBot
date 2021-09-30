using Discord;
using ImageMagick;
using ProtoBuf;
using StarcoreDiscordBot.Networking;
using System;
using System.Runtime.Serialization;

namespace StarcoreDiscordBot
{
    [ProtoContract]
    [DataContract(Name = "TeamColor", Namespace = "Math0424")]
    public class TeamColor
    {

        public static TeamColor Default = new TeamColor(0, 0, 0);

        [DataMember] [ProtoMember(1)] public byte R;
        [DataMember] [ProtoMember(2)] public byte G;
        [DataMember] [ProtoMember(3)] public byte B;

        public TeamColor(byte r, byte g, byte b)
        {
            this.R = r;
            this.G = g;
            this.B = b;
        }

        public MyTuple<int, int, int> ToTuple()
        {
            return new MyTuple<int, int, int>(R, G, B);
        }

        public MagickColor ToMagickColor()
        {
            return new MagickColor((ushort)(ushort.MaxValue * (255.0 / R)), (ushort)(ushort.MaxValue * (255.0 / G)), (ushort)(ushort.MaxValue * (255.0 / B)));
        }

        public Color ToDiscordColor()
        {
            return new Color(R, B, G);
        }

    }
}
