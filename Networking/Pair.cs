using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarcoreDiscordBot.Networking
{
    [ProtoContract]
    public class Pair<T, K>
    {
        [ProtoMember(1)] public T item1;
        [ProtoMember(2)] public K item2;
        public Pair(T item1, K item2)
        {
            this.item1 = item1;
            this.item2 = item2;
        }
        public Pair() { }
    }
}
