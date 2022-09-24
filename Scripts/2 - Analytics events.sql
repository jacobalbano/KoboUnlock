drop index if exists analytics_events_timestamp;

create trigger if not exists delete_analytics
after insert on AnalyticsEvents begin
  delete from AnalyticsEvents;
end;