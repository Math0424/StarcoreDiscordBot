using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarcoreDiscordBot.Networking
{
    [ProtoContract]
    public class MyTuple<T, K, V>
    {
        [ProtoMember(1)] public T item1;
        [ProtoMember(2)] public K item2;
        [ProtoMember(3)] public V item3;
        public MyTuple(T item1, K item2, V item3)
        {
            this.item1 = item1;
            this.item2 = item2;
            this.item3 = item3;
        }
        public MyTuple() { }
    }
}
