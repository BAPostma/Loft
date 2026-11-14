using Loft.Function.Maintenance;
using Xunit;

namespace Loft.Function.Tests;

public class ControlCommandTests
{
    [Fact]
    public void ParseCommandWithParameters()
    {
        var command = ControlCommand.ParseFrom("reprocess-inbox\r\nbucket=loft-example.com");

        Assert.Equal("reprocess-inbox", command.Name);
        Assert.Equal("loft-example.com", command["bucket"]);
    }

    [Fact]
    public void ParsePlainCommandName()
    {
        var command = ControlCommand.ParseFrom("receipient-case-normalisation");

        Assert.Equal("receipient-case-normalisation", command.Name);
        Assert.Empty(command.Parameters);
    }

    [Fact]
    public void ParseCommandWithWhitespace()
    {
        var command = ControlCommand.ParseFrom("  reprocess-inbox  \n bucket = loft-example.com \n");

        Assert.Equal("reprocess-inbox", command.Name);
        Assert.Equal("loft-example.com", command["bucket"]);
    }

    [Fact]
    public void ParametersAreCaseInsensitive()
    {
        var command = ControlCommand.ParseFrom("reprocess-inbox\nBUCKET=loft-example.com");

        Assert.Equal("loft-example.com", command["bucket"]);
        Assert.Equal("loft-example.com", command["Bucket"]);
    }

    [Fact]
    public void ParameterReturnsNullWhenMissing()
    {
        var command = ControlCommand.ParseFrom("reprocess-inbox");

        Assert.Null(command["bucket"]);
    }

    [Fact]
    public void ParseEmptyBody()
    {
        var command = ControlCommand.ParseFrom("   \n  ");

        Assert.Equal(string.Empty, command.Name);
        Assert.Empty(command.Parameters);
    }
}
