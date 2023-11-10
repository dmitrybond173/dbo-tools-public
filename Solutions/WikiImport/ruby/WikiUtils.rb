#
# Utils to parse MediaWiki export file and try to import it via WebDriver
#
# by Dmitry Bond. (November 2023)
#

require 'nokogiri'
require 'watir'
require 'CGI'

module WikiUtils

  class WikiPage
    attr_reader :node, :title, :id, :revisions, :text

    def initialize(node)
      @node = node
      puts "--- loading: #{@node.name}"
      @revisions = []

      childs = @node.xpath("./*")
      childs.each do |child|
        puts "  ++ #{child.name}"
        if child.name == "title"
          @title = child.content
          puts "    = title: #{@title}"
        elsif child.name == "id"
          @id = child.content
          puts "    = id: #{@id}"
        elsif child.name == "revision"
          rev = PageRevision.new(self, child)
          @revisions.push(rev)
        end
      end
    end

    def latestRevision
      return revisions[revisions.size-1]
    end

    class PageRevision
      attr_reader :owner, :node, :id, :timestamp, :text
      def initialize(owner, node)
        @owner = owner
        @node = node

        revs = @node.xpath("./*")
        revs.each do |rev|
          puts "    * #{rev.name}"
          if rev.name == "id"
            @id = rev.content
            puts "      = id: #{@id}"
          elsif rev.name == "timestamp"
            @timestamp = rev.content
            puts "      = id: #{@timestamp}"
          elsif rev.name == "text"
            @text = rev.content
            puts "      = txt.len: #{@text.length}"
          end
        end
      end
    end

  end # WikiPage

  # WikiBrowser classed used to create and save wiki pages to specified url
  class WikiBrowser

    attr_reader :wikiBaseUrl, :currentUrl, :currentPage, :browser, :driver

    def initialize(pWikiBaseUrl)
      @wikiBaseUrl = pWikiBaseUrl

      @@urlTemplate = "${WikiBaseUrl}/index.php?title=${PageTitle}&action=edit"
      @@urlTemplate = @@urlTemplate.gsub("${WikiBaseUrl}", pWikiBaseUrl)

      @browser = Watir::Browser.new
      @driver = @browser.driver
    end

    def close()
      if !@browser.nil?
        puts '--- Close browser...'
        @browser.close
      end
    end

    def loginToWiki(pUser, pPassw)
      @currentUrl = @wikiBaseUrl + "/index.php?title=Special:UserLogin"
      puts '--- Open Wiki[' + @currentUrl + ']'
      @browser.goto @currentUrl
      @browser.text_field(:name => "wpName").set pUser
      @browser.text_field(:name => "wpPassword").set pPassw
      @browser.button(:name => "wploginattempt").click
    end

    def openWikiEditor(pPage)
      @currentPage = pPage
      @currentUrl = @@urlTemplate
      title = CGI.escape(@currentPage.title)
      @currentUrl = @currentUrl.gsub("${PageTitle}", title)
      puts '--- Open WikiEditor[' + @currentUrl + ']'
      @browser.goto @currentUrl
    end

    def submitWikiPage()
      txt = @currentPage.latestRevision.text
      chunks = chunk_str(txt, 1024)
      puts "wikiPage[#{@currentPage.title}]: textLen: #{txt.length}, chunks: #{chunks.size}"

      field = @browser.textarea(:id => "wpTextbox1")

      # WARNING!
      # I cannot just do field.set chunk txt because in case of text longer 10kb it mau hang!
      # So, I have to cut text to chunks by 1kb and use .append txt

      idx = 0
      chunks.each do |chunk|
        idx += 1
        if idx == 1
          field.set chunk
        else
          field.append chunk
        end
      end

      puts "...small pause to check the console output..."
      sleep 3.0

      puts "  saving page..."
      @browser.button(:name => "wpSave").click
      puts "  = saved."
    end

    def chunk_str(string, size)
      return (string.length / size).times.collect { |i| string[i * size, size] }
    end

  end # WikiBrowser

end
