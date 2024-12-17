#!/usr/local/bin/Rscript

dht::greeting()

## load libraries without messages or warnings
withr::with_message_sink("/dev/null", library(dplyr))
withr::with_message_sink("/dev/null", library(tidyr))
withr::with_message_sink("/dev/null", library(sf))
withr::with_message_sink("/dev/null", library(jsonlite))
withr::with_message_sink("/dev/null", library(stringr))

doc <- "
      Usage:
      entrypoint_json.R <lat_lon_json>
      "

opt <- docopt::docopt(doc)

json <- opt$lat_lon_json %>%
  str_replace_all(c("[{]" = "{\"", ":" = "\":\"", "," = "\",\"", "[}]" = "\"}", "[}]\",\"[{]" = "},{", "\" " = "\"", " \"" = "\""))
d <- fromJSON(json)
d <- sf::st_as_sf(d, coords = c("lon", "lat"), crs = 4326, remove=F)
d <- sf::st_transform(d, 5072)

## add code here to calculate geomarkers
message("reading tract shapefile...")
tracts10 <- readRDS('/opt/tracts_2010_sf_5072.rds')

message("joining to 2010 TIGER/Line+ census tracts using EPSG:5072 projection")
d_tract <- st_join(d, tracts10) %>%
  st_drop_geometry()

message("reading deprivation index data...")
dep_index18 <- readRDS('/opt/tract_dep_index_18.rds')

message("joining 2018 tract-level deprivation index")
d_tract <- left_join(d_tract, dep_index18, by = c('fips_tract_id' = 'census_tract_fips'))
d_tract <- rename(d_tract, census_tract_id = fips_tract_id)

toJSON(d_tract)