puts "Content-Type: text/plain; charset=UTF-8"
puts "Status: 200"
puts "X-Foo-Header: Bar"

puts 
puts
puts "Hello from ruby!"
puts
puts "ruby version: #{RUBY_VERSION} (#{RUBY_RELEASE_DATE}) [#{RUBY_PLATFORM}]"

puts

puts "### Arguments ###"
puts
ARGV.each_with_index { |e, i| puts "arg #{i}: #{e}" }

puts

puts "### Env Vars ###"
puts
ENV.each { |k, v| puts "#{k}=#{v}" }

puts

puts "### Files ###"
puts
Dir.foreach("/") { |x| puts x }
