using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.FruitDessertTemplates;

namespace skestock.Application.UnitTests.Features.FruitDessertTemplates;

public class WhatsAppFormatterTests
{
    [TestCase("<p><strong>x</strong></p>", "*x*")]
    [TestCase("<p><b>x</b></p>", "*x*")]
    [TestCase("<p><em>x</em></p>", "_x_")]
    [TestCase("<p><i>x</i></p>", "_x_")]
    [TestCase("<p><s>x</s></p>", "~x~")]
    [TestCase("<p><strike>x</strike></p>", "~x~")]
    [TestCase("<p><code>x</code></p>", "`x`")]
    public void ToWhatsApp_MapsEachMark(string html, string expected) =>
        WhatsAppFormatter.ToWhatsApp(html).ShouldBe(expected);

    [Test]
    public void ToWhatsApp_NestedMarks() =>
        WhatsAppFormatter.ToWhatsApp("<p><strong>a <em>b</em> c</strong></p>").ShouldBe("*a _b_ c*");

    [Test]
    public void ToWhatsApp_ParagraphsAndBreaks() =>
        WhatsAppFormatter.ToWhatsApp("<p>a<br>b</p><p>c</p><p></p><p>d</p>").ShouldBe("a\nb\nc\n\nd");

    [Test]
    public void ToWhatsApp_BulletList() =>
        WhatsAppFormatter.ToWhatsApp("<p>t</p><ul><li><p>a</p></li><li>b</li></ul>").ShouldBe("t\n- a\n- b");

    [Test]
    public void ToWhatsApp_OrderedList() =>
        WhatsAppFormatter.ToWhatsApp("<ol><li>a</li><li>b</li></ol>").ShouldBe("1. a\n2. b");

    [Test]
    public void ToWhatsApp_LegacyPlainText_ReturnedUnchanged()
    {
        const string text = "Bună, {ContactPerson}!\nLa revedere & 1 < 2.";
        WhatsAppFormatter.ToWhatsApp(text).ShouldBe(text);
    }

    [Test]
    public void ToWhatsApp_WhitespaceMovedOutsideMarkers() =>
        WhatsAppFormatter.ToWhatsApp("<p>a<strong> b </strong>c</p>").ShouldBe("a *b* c");

    [Test]
    public void ToWhatsApp_EmptyMarkDropped() =>
        WhatsAppFormatter.ToWhatsApp("<p>a<strong></strong><em> </em>b</p>").ShouldBe("a b");

    [Test]
    public void ToWhatsApp_UnclosedMarkIsClosed() =>
        WhatsAppFormatter.ToWhatsApp("<p><strong>a").ShouldBe("*a*");

    [Test]
    public void ToWhatsApp_DecodesEntities() =>
        WhatsAppFormatter.ToWhatsApp("<p>a &amp; b&nbsp;c &lt;d&gt;</p>").ShouldBe("a & b c <d>");

    [Test]
    public void ToWhatsApp_DropsScriptAndKeepsUnknownTagText() =>
        WhatsAppFormatter.ToWhatsApp("<p>a<script>alert(1)</script><div>b</div></p>").ShouldBe("ab");

    [Test]
    public void ToWhatsApp_BrokenPlaceholderStaysText() =>
        WhatsAppFormatter.ToWhatsApp("<p><strong>{Da</strong>te}</p>").ShouldBe("*{Da*te}");

    [Test]
    public void Sanitize_KeepsWhitelistWithoutAttributes() =>
        WhatsAppFormatter.Sanitize("<p class=\"x\" style=\"color:red\">a<strong onclick=\"x()\">b</strong><br/></p>")
            .ShouldBe("<p>a<strong>b</strong><br></p>");

    [Test]
    public void Sanitize_DropsScriptAndStyleContent() =>
        WhatsAppFormatter.Sanitize("<p>a</p><script>alert(1)</script><style>p{}</style>b").ShouldBe("<p>a</p>b");

    [Test]
    public void Sanitize_ImgOnerror_DropsTag() =>
        WhatsAppFormatter.Sanitize("<p>a<img src=x onerror=alert(1)>b</p>").ShouldBe("<p>ab</p>");

    [Test]
    public void Sanitize_JavascriptLink_KeepsTextOnly() =>
        WhatsAppFormatter.Sanitize("<a href=\"javascript:alert(1)\">click</a>").ShouldBe("click");

    [Test]
    public void Sanitize_DropsComments() =>
        WhatsAppFormatter.Sanitize("a<!-- <script>x</script> -->b<!-- unclosed").ShouldBe("ab");

    [Test]
    public void Sanitize_UnclosedTag_DoesNotLeakMarkup() =>
        WhatsAppFormatter.Sanitize("<p>a</p><img src=\"x").ShouldBe("<p>a</p>");

    [Test]
    public void Sanitize_EncodesText() =>
        WhatsAppFormatter.Sanitize("<p>a &amp; b &lt;script&gt; 1 < 2</p>")
            .ShouldBe("<p>a &amp; b &lt;script&gt; 1 &lt; 2</p>");
}
