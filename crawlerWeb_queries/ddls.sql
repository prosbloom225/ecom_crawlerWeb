drop table crawlerDB.dbo.missingImageDump
drop table crawlerDB.dbo.crawlLog
drop table crawlerDB.dbo.crawlIDs
drop table crawlerDB.dbo.crawlStats
drop table crawlerDB.dbo.crawlStats_tmp
drop table crawlerDB.dbo.missingImageManifest

create table crawlerDB.dbo.missingImageDump (url varchar(255), nav_from varchar(255), crawlID int, timestamp datetime, imageName varchar(255), type varchar(255), color varchar(255))
create table errorsDump(type varchar(255), url varchar(255), nav_from varchar(255), crawlID int, timestamp datetime)
create table crawlerDB.dbo.missingImageManifest (url varchar(255), nav_from varchar(255), crawlID int, timestamp datetime, imageName varchar(255), type varchar(255), color varchar(255), productID varchar(15), dept varchar(15))
create table crawlerDB.dbo.crawlLog (message varchar(255), timestamp datetime, crawlID int)
create table crawlerDB.dbo.crawlIDs (crawlID int)
create table crawlerDB.dbo.crawlStats (crawlID int, pagesVisited int, productsVisited int, missingImages int, elapsed bigint)
create table crawlerDB.dbo.crawlStats_tmp (crawlID int, pagesVisited int, productsVisited int, missingImages int, elapsed bigint)
create table crawlerDB.dbo.


insert into crawlerDB.dbo.crawlIDs values (0)


select * from crawlLog order by crawlID desc
select count(1), crawlId from missingImageDump where crawlID = (select max(crawlID) from crawlIDs) group by crawlId

select * from crawlIDs order by crawlid desc


