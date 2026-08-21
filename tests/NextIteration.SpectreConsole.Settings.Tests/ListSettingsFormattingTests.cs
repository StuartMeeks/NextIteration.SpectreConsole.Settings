using NextIteration.SpectreConsole.Settings.Commands;

using Xunit;

namespace NextIteration.SpectreConsole.Settings.Tests
{
    public sealed class ListSettingsFormattingTests
    {
        private sealed class Nested
        {
            public string Name { get; set; } = "x";
            public int Age { get; set; } = 3;
        }

        private sealed class Holder
        {
            public string Text { get; set; } = "hello";
            public bool Flag { get; set; } = true;
            public Nested Complex { get; set; } = new();
            public List<string> Items { get; set; } = ["a", "b"];
        }

        private static string Format(string propertyName) =>
            ListSettingsCommand.FormatValue(typeof(Holder).GetProperty(propertyName)!, new Holder());

        [Fact]
        public void Scalar_String_RendersPlainly() => Assert.Equal("hello", Format(nameof(Holder.Text)));

        [Fact]
        public void Scalar_Bool_RendersPlainly() => Assert.Equal("True", Format(nameof(Holder.Flag)));

        [Fact]
        public void ComplexObject_RendersCompactJson() =>
            Assert.Equal("{\"Name\":\"x\",\"Age\":3}", Format(nameof(Holder.Complex)));

        [Fact]
        public void Collection_RendersCompactJson() =>
            Assert.Equal("[\"a\",\"b\"]", Format(nameof(Holder.Items)));
    }
}
