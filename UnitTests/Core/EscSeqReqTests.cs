using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Terminal.Gui.CoreTests {
	public class EscSeqReqTests {
		[Fact]
		public void Constructor_Defaults ()
		{
			var escSeqReq = new EscSeqReqProc ();
			Assert.NotNull (escSeqReq.EscSeqReqStats);
			Assert.Empty (escSeqReq.EscSeqReqStats);
		}

		[Fact]
		public void Add_Tests ()
		{
			var escSeqReq = new EscSeqReqProc ();
			escSeqReq.Add ("t");
			Assert.Single (escSeqReq.EscSeqReqStats);
			Assert.Equal ("t", escSeqReq.EscSeqReqStats [^1].Terminating);
			Assert.Equal (1, escSeqReq.EscSeqReqStats [^1].NumRequests);
			Assert.Equal (1, escSeqReq.EscSeqReqStats [^1].NumOutstanding);

			escSeqReq.Add ("t", 2);
			Assert.Single (escSeqReq.EscSeqReqStats);
			Assert.Equal ("t", escSeqReq.EscSeqReqStats [^1].Terminating);
			Assert.Equal (1, escSeqReq.EscSeqReqStats [^1].NumRequests);
			Assert.Equal (1, escSeqReq.EscSeqReqStats [^1].NumOutstanding);

			escSeqReq = new EscSeqReqProc ();
			escSeqReq.Add ("t", 2);
			Assert.Single (escSeqReq.EscSeqReqStats);
			Assert.Equal ("t", escSeqReq.EscSeqReqStats [^1].Terminating);
			Assert.Equal (2, escSeqReq.EscSeqReqStats [^1].NumRequests);
			Assert.Equal (2, escSeqReq.EscSeqReqStats [^1].NumOutstanding);

			escSeqReq.Add ("t", 3);
			Assert.Single (escSeqReq.EscSeqReqStats);
			Assert.Equal ("t", escSeqReq.EscSeqReqStats [^1].Terminating);
			Assert.Equal (2, escSeqReq.EscSeqReqStats [^1].NumRequests);
			Assert.Equal (2, escSeqReq.EscSeqReqStats [^1].NumOutstanding);
		}

		[Fact]
		public void Remove_Tests ()
		{
			var escSeqReq = new EscSeqReqProc ();
			escSeqReq.Add ("t");
			escSeqReq.Remove ("t");
			Assert.Empty (escSeqReq.EscSeqReqStats);

			escSeqReq.Add ("t", 2);
			escSeqReq.Remove ("t");
			Assert.Single (escSeqReq.EscSeqReqStats);
			Assert.Equal ("t", escSeqReq.EscSeqReqStats [^1].Terminating);
			Assert.Equal (2, escSeqReq.EscSeqReqStats [^1].NumRequests);
			Assert.Equal (1, escSeqReq.EscSeqReqStats [^1].NumOutstanding);

			escSeqReq.Remove ("t");
			Assert.Empty (escSeqReq.EscSeqReqStats);
		}

		[Fact]
		public void Requested_Tests ()
		{
			var escSeqReq = new EscSeqReqProc ();
			Assert.False (escSeqReq.Requested ("t"));

			escSeqReq.Add ("t");
			Assert.False (escSeqReq.Requested ("r"));
			Assert.True (escSeqReq.Requested ("t"));
		}
	}
}
