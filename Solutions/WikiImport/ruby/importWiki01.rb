#
# Simple/stright-forward solution to parse MediaWiki export file and try to import it via WebDriver
#
# by Dmitry Bond. (November 2023)
#
# See also:
#  * https://linuxhint.com/parse-xml-in-ruby/

require 'nokogiri'
require 'watir'
require './WikiUtils'

#src_file = "C:/sbx/dbo-tools-public/Solutions/WikiImport/My+Wiki-sm.xml" # <- small pice of wiki for debug
src_file = "C:/sbx/dbo-tools-public/Solutions/WikiImport/My+Wiki-sm.xml" # <- full wiki exported

wiki_base_url = "http://localhost:8080/w"
wiki_user = "dmitrybond"
wiki_password = "Forget1234"

def chunk(string, size)
  (string.length / size).times.collect { |i| string[i * size, size] }
end

def test
  s = "Hello, world! how are you? what are the weather today?"
  a = chunk(s, 10)
  puts a
end

#return test

# Load XML exported from wiki
xml_text = File.open(src_file)
wiki_dom  = Nokogiri::XML(xml_text)
wiki_dom.remove_namespaces!
#puts wiki_dom

# select wiki-page nodes from input XML
puts wiki_dom.xpath("/mediawiki/@version")
pagesNodes = wiki_dom.xpath("/mediawiki/page")
puts pagesNodes.size

# deserialize wiki pages from list of page DOM nodes
pages = []
pagesNodes.each do |node|
  pg = WikiUtils::WikiPage.new(node)
  pages.push(pg)
end

# print count of deserialized pages
puts "#{pages.size} pages recognized"

# saving loaded pages to browser
idx = 0
saver = WikiUtils::WikiBrowser.new(wiki_base_url)
saver.loginToWiki(wiki_user, wiki_password)

pages.each do |page|
  idx += 1
  #next if idx == 1

  puts "+++ Processing page [#{page.title}] ..."
  saver.openWikiEditor(page)
  saver.submitWikiPage()
  puts "  + completed."

  sleep 1.0

  #puts "Press [Enter] to continue with next..."
  #s = gets
  #break idx > 7
end
