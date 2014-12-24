using Lucene.Net.Analysis;
using Lucene.Net.TestFramework.Analysis;

namespace Lucene.Net.Test.Analysis
{
	/// <summary>Trivial position class.</summary>
	
	public class TestPosition : Position
	{
		private string fact;

	    public TestPosition(TokenStream input) //: base(input)
	    {
	    }

	    public string Fact { get; set; }

        //public override bool IncrementToken()
        //{
        //    throw new System.NotImplementedException();
        //}

        //protected override Position NewPosition()
        //{
        //    throw new System.NotImplementedException();
        //}
	}
}
