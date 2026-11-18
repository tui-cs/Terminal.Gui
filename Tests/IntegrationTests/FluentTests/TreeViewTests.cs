using AppTestHelpers;
using AppTestHelpers.XunitHelpers;

namespace IntegrationTests;

public class TreeViewTests (ITestOutputHelper outputHelper) : TestsAllDrivers
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void TreeView_AllowReOrdering (string d)
    {
        var tv = new TreeView { Width = Dim.Fill (), Height = Dim.Fill () };

        TreeNode car;
        TreeNode lorry;
        TreeNode bike;

        var root = new TreeNode
        {
            Text = "Root", Children = [car = new TreeNode { Text = "Car" }, lorry = new TreeNode { Text = "Lorry" }, bike = new TreeNode { Text = "Bike" }]
        };
        tv.AddObject (root);

        using AppTestHelper helper = With.A<Window> (40, 10, d, _out)
                                         .Add (tv)
                                         .Focus (tv)
                                         .WaitIteration ()
                                         .ScreenShot ("Before expanding", _out)
                                         .AssertEqual (root, tv.GetObjectOnRow (0))
                                         .AssertNull (tv.GetObjectOnRow (1))
                                         .KeyDown (Key.CursorRight)
                                         .ScreenShot ("After expanding", _out)
                                         .AssertMultiple (() =>
                                                          {
                                                              Assert.Equal (root, tv.GetObjectOnRow (0));
                                                              Assert.Equal (car, tv.GetObjectOnRow (1));
                                                              Assert.Equal (lorry, tv.GetObjectOnRow (2));
                                                              Assert.Equal (bike, tv.GetObjectOnRow (3));
                                                          })
                                         .AssertIsAssignableFrom<ITreeNode> (tv.SelectedObject)
                                         .Then (_ =>
                                                {
                                                    // Re order
                                                    root.Children = [bike, car, lorry];
                                                    tv.RefreshObject (root);
                                                })
                                         .WaitIteration ()
                                         .ScreenShot ("After re-order", _out)
                                         .AssertMultiple (() =>
                                                          {
                                                              Assert.Equal (root, tv.GetObjectOnRow (0));
                                                              Assert.Equal (bike, tv.GetObjectOnRow (1));
                                                              Assert.Equal (car, tv.GetObjectOnRow (2));
                                                              Assert.Equal (lorry, tv.GetObjectOnRow (3));
                                                          })
                                         .WriteOutLogs (_out);

        helper.Stop ();
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void TreeViewReOrder_PreservesExpansion (string d)
    {
        var tv = new TreeView { Width = Dim.Fill (), Height = Dim.Fill () };

        TreeNode car;
        TreeNode lorry;
        TreeNode bike;

        TreeNode mrA;
        TreeNode mrB;

        TreeNode mrC;

        TreeNode mrD;
        TreeNode mrE;

        var root = new TreeNode
        {
            Text = "Root",
            Children =
            [
                car = new TreeNode { Text = "Car", Children = [mrA = new TreeNode { Text = "Mr A" }, mrB = new TreeNode { Text = "Mr B" }] },
                lorry = new TreeNode { Text = "Lorry", Children = [mrC = new TreeNode { Text = "Mr C" }] },
                bike = new TreeNode { Text = "Bike", Children = [mrD = new TreeNode { Text = "Mr D" }, mrE = new TreeNode { Text = "Mr E" }] }
            ]
        };

        tv.AddObject (root);
        tv.ExpandAll ();

        using AppTestHelper helper = With.A<Window> (40, 13, d)
                                         .Add (tv)
                                         .WaitIteration ()
                                         .ScreenShot ("Initial State", _out)
                                         .AssertMultiple (() =>
                                                          {
                                                              Assert.Equal (root, tv.GetObjectOnRow (0));
                                                              Assert.Equal (car, tv.GetObjectOnRow (1));
                                                              Assert.Equal (mrA, tv.GetObjectOnRow (2));
                                                              Assert.Equal (mrB, tv.GetObjectOnRow (3));
                                                              Assert.Equal (lorry, tv.GetObjectOnRow (4));
                                                              Assert.Equal (mrC, tv.GetObjectOnRow (5));
                                                              Assert.Equal (bike, tv.GetObjectOnRow (6));
                                                              Assert.Equal (mrD, tv.GetObjectOnRow (7));
                                                              Assert.Equal (mrE, tv.GetObjectOnRow (8));
                                                          })
                                         .Then (_ =>
                                                {
                                                    // Re order
                                                    root.Children = [bike, car, lorry];
                                                    tv.RefreshObject (root);
                                                })
                                         .WaitIteration ()
                                         .ScreenShot ("After re-order", _out)
                                         .AssertMultiple (() =>
                                                          {
                                                              Assert.Equal (root, tv.GetObjectOnRow (0));
                                                              Assert.Equal (bike, tv.GetObjectOnRow (1));
                                                              Assert.Equal (mrD, tv.GetObjectOnRow (2));
                                                              Assert.Equal (mrE, tv.GetObjectOnRow (3));
                                                              Assert.Equal (car, tv.GetObjectOnRow (4));
                                                              Assert.Equal (mrA, tv.GetObjectOnRow (5));
                                                              Assert.Equal (mrB, tv.GetObjectOnRow (6));
                                                              Assert.Equal (lorry, tv.GetObjectOnRow (7));
                                                              Assert.Equal (mrC, tv.GetObjectOnRow (8));
                                                          })
                                         .WriteOutLogs (_out);

        helper.Stop ();
    }
}
