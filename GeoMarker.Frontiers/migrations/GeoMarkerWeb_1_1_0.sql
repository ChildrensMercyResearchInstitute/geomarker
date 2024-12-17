ALTER TABLE `geomarker`.`recordsprocessed` 
ADD COLUMN `Status` INT NOT NULL AFTER `Records`;

UPDATE `geomarker`.`recordsprocessed` processed
	JOIN `geomarker`.`requests` requested 
	ON processed.RequestGuid = requested.Guid AND processed.Status=2
    SET processed.Status=2;


UPDATE `geomarker`.`recordsprocessed` processed
	JOIN `geomarker`.`requests` requested
	ON processed.RequestGuid = requested.Guid AND processed.Status=3
    SET processed.Status=3;
