#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using MAVLinkAPI.Ext;

namespace MAVLinkAPI.API
{
    public abstract class Pipe<TIn, TOut> : IPipe<TIn, TOut>
    {
        /**
         * A number representing the cumulated unprocessed data (due to computing power or backpressure)
         *
         * for a composed pipe, this number should be the sum of the pressures of all components
         */
        public virtual int Pressure => 0;

        public List<TOut> Process(TIn input)
        {
            return ProcessOrNull(input) ?? new List<TOut>();
        }

        public List<TOut>? ProcessOrNull(TIn input)
        {
            return PrimaryFn(input) ?? FallbackFn(input);
        }

        protected abstract List<TOut>? PrimaryFn(TIn input);

        protected virtual List<TOut>? FallbackFn(TIn input)
        {
            return null;
        }

        IEnumerable<TOut>? IPipe<TIn, TOut>.ProcessOrNull(TIn input)
        {
            return ProcessOrNull(input);
        }

        IEnumerable<TOut> IPipe<TIn, TOut>.Process(TIn input)
        {
            return Process(input);
        }

        public Pipe<TIn, TOut> Cat(Pipe<TIn, TOut> that)
        {
            return new CatT
            {
                Left = this,
                Right = that
            };
        }

        public Pipe<TIn, TOut> Union(Pipe<TIn, TOut> that)
        {
            return new UnionT
            {
                Left = this,
                Right = that
            };
        }

        public Pipe<TIn, TOut> OrElse(Pipe<TIn, TOut> that)
        {
            return new OrElseT
            {
                Left = this,
                Right = that
            };
        }

        public Pipe<TIn, T2> SelectMany<T2>(Func<TIn, TOut, List<T2>> fn)
        {
            return new CutElimination<T2>
            {
                Prev = this,
                Fn = fn
            };
        }

        public Pipe<TIn, T2> Select<T2>(Func<TIn, TOut, T2> fn)
        {
            return SelectMany((ii, x) => new List<T2> { fn(ii, x) });
        }

        /**
         * discard all output and return a Pipe that always returns empty
         */
        public Pipe<TIn, T2> Discard<T2>(Pipe<TIn, TOut> reader)
        {
            throw new NotImplementedException();
        }

        public abstract class LeftAndRight<TLeft, TRight> : Pipe<TIn, TOut>
            where TLeft : Pipe<TIn, TOut>
            where TRight : Pipe<TIn, TOut>
        {
            public TLeft Left = null!;
            public TRight Right = null!;

            public override int Pressure => Left.Pressure + Right.Pressure;
        }

        private sealed class CatT : LeftAndRight<Pipe<TIn, TOut>, Pipe<TIn, TOut>>
        {
            protected override List<TOut>? PrimaryFn(TIn input)
            {
                return Left.ProcessOrNull(input)
                    .ConcatNullSafe(Right.ProcessOrNull(input))
                    ?.ToList();
            }
        }

        /**
         * similar to CatT, but duplicates are removed
         */
        private sealed class UnionT : LeftAndRight<Pipe<TIn, TOut>, Pipe<TIn, TOut>>
        {
            protected override List<TOut>? PrimaryFn(TIn input)
            {
                return Left.ProcessOrNull(input)
                    .UnionNullSafe(Right.ProcessOrNull(input))
                    ?.ToList();
            }
        }

        private sealed class OrElseT : LeftAndRight<Pipe<TIn, TOut>, Pipe<TIn, TOut>>
        {
            protected override List<TOut>? PrimaryFn(TIn input)
            {
                return Left.ProcessOrNull(input) ?? Right.ProcessOrNull(input);
            }
        }

        private sealed class CutElimination<T2> : Pipe<TIn, T2>
        {
            public Pipe<TIn, TOut> Prev = null!;
            public Func<TIn, TOut, List<T2>> Fn = null!;

            public override int Pressure => Prev.Pressure;

            protected override List<T2>? PrimaryFn(TIn input)
            {
                var prevV = Prev.ProcessOrNull(input);
                if (prevV == null) return null;

                return prevV.SelectMany(v => Fn(input, v)).ToList();
            }
        }
    }

    public static class PipeExtensions
    {
        public static Pipe<Ti2, To2> Upcast<Ti2, To2, TIn, TOut>(this Pipe<TIn, TOut> left)
            where TOut : To2
            where Ti2 : TIn
        {
            // TODO: if IPipe with variance is used everywhere it won't be needed
            return new UpcastPipe<Ti2, To2, TIn, TOut>
            {
                Prev = left
            };
        }

        sealed class UpcastPipe<Ti2, To2, TIn, TOut> : Pipe<Ti2, To2>
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
}