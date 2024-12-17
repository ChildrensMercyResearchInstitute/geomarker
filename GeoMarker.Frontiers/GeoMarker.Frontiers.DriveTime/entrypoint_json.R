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
      entrypoint_json.R <lat_lon_json> <site>
      "

opt <- docopt::docopt(doc)

centers <- readr::read_csv('/app/center_addresses.csv')
selected_site <- opt$site

if (! selected_site %in% centers$abbreviation){
  stop('site argument is invalid or missing; please consult documentation for details', call. = FALSE)
}

json <- opt$lat_lon_json %>%
  str_replace_all(c("[{]" = "{\"", ":" = "\":\"", "," = "\",\"", "[}]" = "\"}", "[}]\",\"[{]" = "},{", "\" " = "\"", " \"" = "\""))
d <- fromJSON(json)
d <- sf::st_as_sf(d, coords = c("lon", "lat"), crs = 4326, remove=F)
d <- sf::st_transform(d, 5072)

message('loading isochrone shape file...')
isochrones <- readRDS(glue::glue("/app/isochrones/{selected_site}_isochrones.rds")) # 5072 projection

## add code here to calculate geomarkers
message('finding drive time for each point...')
d <- suppressWarnings( st_join(d, isochrones, largest = TRUE) ) %>% 
  mutate(drive_time = ifelse(!is.na(drive_time), as.character(drive_time), "> 60"))

message('finding distance (m) for each point...')

centers <- centers %>% 
  filter(abbreviation == selected_site) %>% 
  st_as_sf(coords = c('lon', 'lat'), crs = 4326) %>%
  st_transform(5072)
centers <- centers[rep(seq_len(nrow(centers)), each = nrow(d)), ]

d$distance <-
  st_distance(centers,
              d,
              by_element = T)

df = as.data.frame(d)
toJSON(subset(df, select = -c(geometry) ), force=T)
