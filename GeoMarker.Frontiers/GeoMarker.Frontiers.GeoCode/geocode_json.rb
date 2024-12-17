require 'rubygems'
require 'geocoder/us'
require 'json'

db = Geocoder::US::Database.new("/opt/geocoder.db")

ARGV[0].split(";").each do |address|
  address_obj = address.split("|")
  geocoded = db.geocode(address_obj[1])[0]
  geocoded["id"] = address_obj[0] unless geocoded.nil?
  $stdout.puts geocoded.to_json
end