select * from crawlLog order by crawlID desc
select count(1) as [missingImages], crawlId from missingImageDump where crawlID = (select max(crawlID) from crawlIDs) group by crawlId

select * from crawlIDs order by crawlid desc