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
        entrypoint_json.R <lat_lon_json> [<census_year>]
        "

opt <- docopt::docopt(doc)

if (is.null(opt$census_year)) {
  opt$census_year <- 2010
  cli::cli_alert("No census year provided. Using 2010.")
}

if(opt$census_year == 2010 | opt$census_year == 2020) {
  dht::check_ram(5)
}

if(! opt$census_year %in% c('2020', '2010', '2000', '1990', '1980', '1970')) {
  cli::cli_alert_danger('Available census geographies include years 1970, 1980, 1990, 2000, 2010, and 2020.')
  stop()
}

if(opt$census_year %in% c('1980', '1970')) {
  cli::cli_alert_warning('Block groups are not available for the selected year. Only tract identifiers will be returned.')
}

message("reading input json...")
json <- opt$lat_lon_json %>%
  str_replace_all(c("[{]" = "{\"", ":" = "\":\"", "," = "\",\"", "[}]" = "\"}", "[}]\",\"[{]" = "},{", "\" " = "\"", " \"" = "\""))
d <- fromJSON(json)
d <- sf::st_as_sf(d, coords = c("lon", "lat"), crs = 4326, remove=F)
d <- sf::st_transform(d, 5072)

message('loading census shape files...')
if (opt$census_year %in% c('1980', '1970')) {
  geography <- readRDS(file=paste0("/app/tracts_", opt$census_year, "_5072.rds"))
} else {
  geography <- readRDS(file=paste0("/app/block_groups_", opt$census_year, "_5072.rds"))
}

message('finding containing geography for each point...')
d <- suppressWarnings( sf::st_join(d, geography, left = FALSE, largest = TRUE) )

if(! opt$census_year %in% c('1980', '1970')) {
  d <- d %>%
    mutate_at(vars(starts_with(glue::glue('census_block_group_id_{opt$census_year}'))),
              list(census_tract_id = ~stringr::str_sub(.x, 1, 11)))
}

df = as.data.frame(d)
toJSON(subset(df, select = -c(geometry) ))