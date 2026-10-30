using System.Windows;

namespace KeepMeAlive.Tests;

public sealed class WindowWorkAreaTests
{
    [Fact]
    public void BottomTaskbarLimitsMaximizedHeightToTheWorkArea()
    {
        var geometry = WindowWorkArea.CalculateBounds(new Rect(0, 0, 1920, 1080),
            new Rect(0, 0, 1920, 1032), new Size(860, 580), new DpiScale(1, 1));
        Assert.Equal(new Rect(0, 0, 1920, 1032), geometry.Bounds);
        Assert.Equal(new Size(860, 580), geometry.MinimumTrackSize);
    }

    [Fact]
    public void LeftTaskbarOffsetsTheMaximizedWindowWithinItsMonitor()
    {
        var geometry = WindowWorkArea.CalculateBounds(new Rect(1920, 0, 2560, 1440),
            new Rect(2000, 0, 2480, 1440), new Size(860, 580), new DpiScale(1, 1));
        Assert.Equal(new Rect(80, 0, 2480, 1440), geometry.Bounds);
    }

    [Fact]
    public void NegativeMonitorCoordinatesRemainRelativeToThatMonitor()
    {
        var geometry = WindowWorkArea.CalculateBounds(new Rect(-1920, -1080, 1920, 1080),
            new Rect(-1880, -1040, 1880, 1040), new Size(860, 580), new DpiScale(1, 1));
        Assert.Equal(new Rect(40, 40, 1880, 1040), geometry.Bounds);
    }

    [Theory]
    [InlineData(1, 860, 580)]
    [InlineData(1.25, 1075, 725)]
    [InlineData(1.5, 1290, 870)]
    [InlineData(2, 1720, 1160)]
    public void MinimumTrackingSizeScalesFromDipsToPhysicalPixels(double scale, int width, int height)
    {
        var geometry = WindowWorkArea.CalculateBounds(new Rect(0, 0, 3840, 2160),
            new Rect(0, 0, 3840, 2080), new Size(860, 580), new DpiScale(scale, scale));
        Assert.Equal(new Size(width, height), geometry.MinimumTrackSize);
        Assert.Equal(new Rect(0, 0, 3840, 2080), geometry.Bounds);
    }

    [Fact]
    public void FractionalDpiRoundsMinimumDimensionsUp()
    {
        var geometry = WindowWorkArea.CalculateBounds(new Rect(0, 0, 2560, 1440),
            new Rect(0, 0, 2560, 1392), new Size(860, 580), new DpiScale(1.33, 1.33));
        Assert.Equal(new Size(1144, 772), geometry.MinimumTrackSize);
    }
}
