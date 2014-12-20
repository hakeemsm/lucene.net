//using System;

//namespace Lucene.Net.TestFramework.Util
//{
//    /// <summary>
//    /// A
//    /// <see cref="NUnit.Framework.Rules.TestRule">NUnit.Framework.Rules.TestRule</see>
//    /// that guarantees the execution of
//    /// <see cref="After()">After()</see>
//    /// even
//    /// if an exception has been thrown from delegate
//    /// <see cref="NUnit.Framework.Runners.Model.Statement">NUnit.Framework.Runners.Model.Statement
//    /// 	</see>
//    /// . This is much
//    /// like
//    /// <see cref="NUnit.Framework.AfterClass">NUnit.Framework.AfterClass</see>
//    /// or
//    /// <see cref="NUnit.Framework.TearDown">NUnit.Framework.TearDown</see>
//    /// annotations but can be used with
//    /// <see cref="NUnit.Framework.Rules.RuleChain">NUnit.Framework.Rules.RuleChain</see>
//    /// to guarantee the order of execution.
//    /// </summary>
//    internal abstract class AbstractBeforeAfterRule : TestRule
//    {
//        public virtual Statement Apply(Statement s, Description d)
//        {
//            return new _Statement_39(this, s);
//        }

//        private sealed class _Statement_39 : Statement
//        {
//            public _Statement_39(AbstractBeforeAfterRule _enclosing, Statement s)
//            {
//                this._enclosing = _enclosing;
//                this.s = s;
//            }

//            /// <exception cref="System.Exception"></exception>
//            public override void Evaluate()
//            {
//                AList<Exception> errors = new AList<Exception>();
//                try
//                {
//                    this._enclosing.Before();
//                    s.Evaluate();
//                }
//                catch (Exception t)
//                {
//                    errors.AddItem(t);
//                }
//                try
//                {
//                    this._enclosing.After();
//                }
//                catch (Exception t)
//                {
//                    errors.AddItem(t);
//                }
//                MultipleFailureException.AssertEmpty(errors);
//            }

//            private readonly AbstractBeforeAfterRule _enclosing;

//            private readonly Statement s;
//        }

//        /// <exception cref="System.Exception"></exception>
//        protected internal virtual void Before()
//        {
//        }

//        /// <exception cref="System.Exception"></exception>
//        protected internal virtual void After()
//        {
//        }
//    }
//}
