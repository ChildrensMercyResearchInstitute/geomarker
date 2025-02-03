
$MySQLDumpPath = "C:\MySQL_5_7\mysql-5.7.43-winx64\bin\"
$DbHost = "s-cmri-mysql-geomarker.mysql.database.azure.com"
$DbUser = "geomarkerroot"
$DbPass = ""
$Db = ""

Write-Host "Testing path for MySQL components"
if( Test-Path $MySQLDumpPath ) {
    Write-Host "Appending MySQL Path"
    $env:Path += ";$MySQLDumpPath"
}
else {
    Write-Error "MySQL utilites not found"
}

Write-Host "Beginning MySQL dump for $Db at $DbHost"
$Date = $(((get-date).ToUniversalTime()).ToString("yyyyMMddTHHmmssZ"))
mysqldump --user=$DbUser --password=$DbPass --host=$DbHost --port=3306 --protocol=tcp --skip-triggers --column-statistics=0 $Db > "backup_$Date.sql"