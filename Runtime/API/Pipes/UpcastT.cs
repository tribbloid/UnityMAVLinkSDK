#nullable enable
using System.Collections.Generic;

namespace MAVLinkSDK.API.Pipes
{
    public class UpcastT<Ti2, To2, TIn, TOut> : Pipe<Ti2, To2>
        where TOut : To2
        where Ti2 : TIn
    {
        public Pipe<TIn, TOut> Prev = null!;

        public override int Pressure => Prev.Pressure;

        protected override List<To2>? PrimaryFn(Ti2 input)
        {
            var prevV = Prev.ProcessOrNull((TIn)input);
            if (prevV == null) return null;
            return prevV.ConvertAll(v => (To2)v);
        }
    }
}