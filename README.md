# Xenogears Tweet by Tweet

This is an azure function app that will tweet the Xenogears script one line at a time, using the script written by [Sheamon on Gamefaqs](https://gamefaqs.gamespot.com/ps/199365-xenogears/faqs/10004).

The function runs every 10 minutes and gets the next line of the script stored in a Sqlite database that is also deployed. The index counter is another table, this approach was used in case the function has to be reset or stopped and the next line is tweeted properly on the next start.