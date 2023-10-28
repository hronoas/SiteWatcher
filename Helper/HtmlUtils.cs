using HtmlAgilityPack;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Documents;

namespace SiteWatcher{


  public static class HtmlUtilities{
    //here's the extension method I use
    private static HtmlNodeCollection SafeSelectNodes(this HtmlNode node, string selector){
          return (node.SelectNodes(selector) ?? new HtmlNodeCollection(node));
    }

    public static string FormatLineBreaks(string html, string savelinks_base=""){
        //first - remove all the existing '\n' from HTML
        //they mean nothing in HTML, but break our logic
        string[] remove = { @"(?<=[a-z]>)\s+|(?<=<[a-z]+[^>]*>)\s+(?=<[a-z]+[^>]*>)", @"\s*<!--.*?-->\s*" }; //@"^[\s]+(?=<)|(?<=>)[\s]+$|\r",@"(?<=>)[\s]+(?=</?(?:t(?:[rdh]|body)))",@"(?<=>)[\r\n]+(?=<)" };
        string[] replace = { @"[\n\t]+"};
        foreach(string rem in remove){
          Regex replacer = new Regex(rem);
          html = replacer.Replace(html,String.Empty);
        }
        foreach(string rep in replace){
          Regex replacer = new Regex(rep);
          html = replacer.Replace(html," ");
        }

        //now create an Html Agile Doc object
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(html);

        //remove comments, head, style and script tags
        foreach (HtmlNode node in doc.DocumentNode.SafeSelectNodes("//comment() | //script | //style | //head"))
        {
            node.ParentNode.RemoveChild(node);
        }

        //now remove all "meaningless" inline elements like "span"
        foreach (HtmlNode node in doc.DocumentNode.SafeSelectNodes("//span | //label")) //add "b", "i" if required
        {
            node.ParentNode.ReplaceChild(HtmlNode.CreateNode(node.InnerHtml), node);
        }

        //block-elements - convert to line-breaks
        foreach (HtmlNode node in doc.DocumentNode.SafeSelectNodes("//p | //div | //li")) //you could add more tags here
        {
            //we add a "\n" ONLY if the node contains some plain text as "direct" child
            //meaning - text is not nested inside children, but only one-level deep

            //use XPath to find direct "text" in element
            var txtNode = node.SelectSingleNode("text()");

            //no "direct" text - NOT ADDDING the \n !!!!
            if (txtNode == null || txtNode.InnerHtml.Trim() == "") continue;

            //"surround" the node with line breaks
            node.ParentNode.InsertBefore(doc.CreateTextNode("\r\n"), node);
            node.ParentNode.InsertAfter(doc.CreateTextNode("\r\n"), node);
        }

        foreach (HtmlNode node in doc.DocumentNode.SafeSelectNodes("//tr")){
            //use XPath to find direct "text" in element
            var txNode = node.SelectSingleNode("text()");
            node.ParentNode.InsertAfter(doc.CreateTextNode("\r\n"), node);
        }

        //todo: might need to replace multiple "\n\n" into one here, I'm still testing...

        //now BR tags - simply replace with "\n" and forget
        foreach (HtmlNode node in doc.DocumentNode.SafeSelectNodes("//br"))
            node.ParentNode.ReplaceChild(doc.CreateTextNode("\r\n"), node);
        
        if(savelinks_base!=""){
          try{
            Uri base_uri = new Uri(savelinks_base);
            foreach (HtmlNode item in doc.DocumentNode.SafeSelectNodes("//a")){
              var herf = (item.Attributes.Where(x => x.Name == "href").FirstOrDefault() as HtmlAttribute)?.Value;
              if(!string.IsNullOrWhiteSpace(herf)){
                Uri link = new Uri(base_uri,herf);
                item.ParentNode.ReplaceChild(doc.CreateTextNode(item.InnerText + string.Format("({0})", link.AbsoluteUri)), item);
              }
            }
          }catch{}
        }

        //finally - return the text which will have our inserted line-breaks in it
        return WebUtility.HtmlDecode(doc.DocumentNode.InnerText.Trim());

        //todo - you should probably add "&code;" processing, to decode all the &nbsp; and such
    }    


    /// <summary>
    /// Converts HTML to plain text / strips tags.
    /// </summary>
    /// <param name="html">The HTML.</param>
    /// <returns></returns>
    public static string ConvertToPlainText(string html, bool savelinks = false)
    {
      HtmlDocument doc = new HtmlDocument();
      doc.LoadHtml(html);
      if (savelinks){
        var nodes =  doc.DocumentNode.SelectNodes("//a");
        if (nodes!=null){ 
          foreach (var item in nodes){
              var herf = ((HtmlAttribute)item.Attributes.Where(x => x.Name == "href").FirstOrDefault()).Value;
              html = html.Replace(item.InnerText, item.InnerText + string.Format("({0})", herf));
          }
          doc.LoadHtml(html);
        }
      }
      StringWriter sw = new StringWriter();
      ConvertTo(doc.DocumentNode, sw);
      sw.Flush();
      return WebUtility.HtmlDecode(sw.ToString().Trim());
    }


    /// <summary>
    /// Count the words.
    /// The content has to be converted to plain text before (using ConvertToPlainText).
    /// </summary>
    /// <param name="plainText">The plain text.</param>
    /// <returns></returns>
    public static int CountWords(string plainText)
    {
      return !String.IsNullOrEmpty(plainText) ? plainText.Split(' ', '\n').Length : 0;
    }


    public static string Cut(string text, int length)
    {
      if (!String.IsNullOrEmpty(text) && text.Length > length)
      {
        text = text.Substring(0, length - 4) + " ...";
      }
      return text;
    }


    private static void ConvertContentTo(HtmlNode node, TextWriter outText)
    {
      foreach (HtmlNode subnode in node.ChildNodes)
      {
        ConvertTo(subnode, outText);
      }
    }


    private static void ConvertTo(HtmlNode node, TextWriter outText)
    {
      string html;
      switch (node.NodeType)
      {
        case HtmlNodeType.Comment:
          // don't output comments
          break;

        case HtmlNodeType.Document:
          ConvertContentTo(node, outText);
          break;

        case HtmlNodeType.Text:
          // script and style must not be output
          string parentName = node.ParentNode.Name;
          if ((parentName == "script") || (parentName == "style"))
            break;

          // get text
          html = ((HtmlTextNode)node).Text;

          // is it in fact a special closing node output as text?
          if (HtmlNode.IsOverlappedClosingElement(html))
            break;

          // check the text is meaningful and not a bunch of whitespaces
          if (html.Trim().Length > 0)
          {
            outText.Write(HtmlEntity.DeEntitize(html));
          }
          break;

        case HtmlNodeType.Element:
          switch (node.Name)
          {
            case "p":
              // treat paragraphs as crlf
              outText.Write("\r\n");
              break;
            case "br":
              outText.Write("\r\n");
              break;
          }

          if (node.HasChildNodes)
          {
            ConvertContentTo(node, outText);
          }
          break;
      }
    }
  }
}