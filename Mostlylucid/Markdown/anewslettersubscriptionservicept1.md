# A Newsletter Subscription Service Pt. 1 - Requirements and Subscription Page

# Introduction
While perusing other people's blogs I noticed a lot of them have a subscription service which allows users to sign up to have an email sent to them weekly with the posts from that blog. 
I decided to implement my own version of this and share how I did it.
[TOC]
<!--category-- ASP.NET, Alpine.js, Email Newsletter -->
<datetime class="hidden">2024-09-21T19:06</datetime>

**NOTE: I don't expect anyone will actually use this, I have a lot of time on my hands after a contract fell through so this keeps me busy!**

# Steps
So to create this service I decided on the following requirements.

1. A flexible subscription page which allows users some flexibility. 
   1. Users only put in email.
   2. The ability to select language of the email.
   3. The ability to select the frequency of the email.
      - If monthly select the day of the month it's sent.
      - If Weekly select the day of the week it's sent.
      - Allow daily (time of day is not important).
      - Allow auto posting to send a mail whenever I blog.
   4. Allow the user to select the categories they are interested in 
5. Allow simple unsubscription
6. Allow preview of the mail they'll receive
7. Allow the user to change their preferences at any time.
2. A separate service which handles the sending of emails.
    - This service will be called by the blog service whenever a new post is made.
    - This service will then send the email to all subscribers.
    - It will use Hangfire to schedule the sending of emails.

The impact of this is that I have a lot of work to do. 

# The Subscription Page
I started off with the fun part, writing a subscription page. I wanted this to work well on desktop as well as mobile browsers (and even the [SPA](/blog/aspnetcorepwa)); from [my Umami analytics](/blog/category/Umami) I can see a fair proportion of users access this stire from mobile devices.

![Umamu Platforms](umamiplatforms.png?format=webp&quality=50&width=600)

I also wanted the Subscription page to be obvious how to use; I'm a big believer in [Steve Krug's "Don't make me think"](https://amzn.to/3XSFGoz) philosophy where a page should be obvious to use and not require the user to think about how to use it.

This means that the defaults should be the majority of users will want to use. I decided on the following defaults:
1. Weekly emails
2. English language
3. All categories selected
4. Email sent on a Monday

I can of course change this later if these prove to be incorrect.

So this is the page I wound up building:

![Subscription Page](subscriptionpage.png)

## The Code for the page
As with the rest of this site I wanted to make the code as simple as possible. It uses the following HTML:

<details>
<summary>Subscription Page</summary>

```html
@using Mostlylucid.Shared
@using Mostlylucid.Shared.Helpers
@model Mostlylucid.EmailSubscription.Models.EmailSubscribeViewModel

<form x-data="{schedule :'@Model.SubscriptionType'}" x-init="$watch('schedule', value => console.log(value))" hx-boost="true" asp-action="Save" asp-controller="EmailSubscription" 
      hx-target="#contentcontainer" hx-swap="#outerHTML">
    <div class="flex flex-col mb-4">
        <div class="flex flex-wrap lg:flex-nowrap lg:space-x-4 space-y-4 lg:space-y-0 items-start">
            <label class="input input-bordered flex items-center gap-2 mb-2 dark:bg-custom-dark-bg bg-white w-full lg:w-2/3">
                <i class='bx bx-envelope'></i>
                <input type="email" class="grow text-black dark:text-white bg-transparent border-0"
                       asp-for="Email" placeholder="Email (optional)"/>
            </label>
            <div class="grid grid-cols-2 sm:grid-cols-[repeat(auto-fit,minmax(100px,1fr))] w-full lg:w-1/3">
                @{
                    var frequency = Enum.GetValues(typeof(SubscriptionType)).Cast<SubscriptionType>().ToList();
                }
                @foreach (var freq in frequency)
                {
                    <div class="flex items-center w-auto h-full min-h-[30px] lg:mb-0 mb-3">
                        <input x-model="schedule" id="@freq" type="radio" value="@freq.ToString()" name="SubscriptionType" class="hidden peer">
                        <label for="@freq" class="ml-2 text-sm font-medium text-white 
                bg-blue-dark border-gray-light border rounded-xl px-1 py-2 w-full 
                peer-checked:text-blue-600 peer-checked:dark:bg-green text-center justify-center">
                            @freq.EnumDisplayName()
                        </label>
                    </div>
                }
            </div>
        </div>
        @{
            var languages = LanguageConverter.LanguageMap;
        }

        <div class="grid grid-cols-[repeat(auto-fit,minmax(85px,1fr))] mt-4 gap-2 pl-6 large:pl-0 w-auto">
            @foreach (var language in languages)
            {
                var isChecked=@Model.Language == language.Key ? "checked" : "";
                <div class="tooltip lg:mb-0 mb-2" data-tip="@language.Value)"  >
                    <div class="flex items-center justify-center w-[85px] h-full min-h-[70px]">
                        <input id="@language.Key" type="radio" value="@language.Key" @isChecked name="language" class="hidden peer">
                        <label for="@language.Key" class="flex flex-col items-center justify-center text-sm font-medium text-white bg-blue-dark opacity-50 peer-checked:opacity-100 w-full h-full">
                            <img src="/img/flags/@(language.Key).svg" asp-append-version="true" class="border-gray-light border rounded-l w-full h-full object-cover" alt="@language.Value">
                        </label>
                    </div>

                </div>
            }
        </div>
        <div class="mt-3 border-neutral-400 dark:border-neutral-600 border rounded-lg" x-data="{ hideCategories: false, showCategories: false }">
            <h4
                class="px-5 py-1 bg-neutral-500 bg-opacity-10 rounded-lg font-body text-primary dark:text-white w-full flex justify-between items-center cursor-pointer"
                x-on:click="if(!hideCategories) { showCategories = !showCategories }">
                <span class="flex flex-row items-center space-x-1">
                    Categories
                    <label class="label cursor-pointer ml-4" x-on:click.stop="">
                        all
                    </label>
                    <input type="checkbox" x-on:click.stop="" x-model="hideCategories" asp-for="AllCategories" class="toggle toggle-info toggle-sm" />
                </span>
                <span>
                    <i
                        class="bx text-2xl"
                        x-show="!hideCategories"
                        :class="showCategories ? 'bx-chevron-up' : 'bx-chevron-down'"></i>
                </span>
            </h4>

            <div class="flex flex-wrap gap-2 pt-2 pl-5 pr-5 pb-2"
                x-show="showCategories"
                x-cloak
                x-transition:enter="max-h-0 opacity-0"
                x-transition:enter-end="max-h-screen opacity-100"
                x-transition:leave="max-h-screen opacity-100"
                x-transition:leave-end="max-h-0 opacity-0">
                <div class="grid grid-cols-[repeat(auto-fit,minmax(150px,1fr))] mt-4 w-full">
                    @foreach (var category in Model.Categories)
                    {
                        var categoryKey = category.Replace(" ", "_").Replace(".", "_").Replace("-", "_");
                        <div class="flex items-center w-auto h-full min-h-[50px]">
                            <input id="@categoryKey" type="checkbox" value="@category"  name="@nameof(Model.SelectedCategories)" class="hidden peer">
                            <label for="@categoryKey" class="ml-2 text-sm font-medium text-white 
            bg-blue-dark border-gray-light border rounded-xl px-1 py-2 w-full 
            peer-checked:text-blue-600 peer-checked:dark:bg-green text-center justify-center">
                                @category
                            </label>
                        </div>
                    }
                </div>
            </div></div>


        <div :class="{ 'opacity-50 pointer-events-none': schedule !== 'Weekly' }" class=" mt-2 border-neutral-400 dark:border-neutral-600 border rounded-lg" >
            <h4
                class="px-5 py-1 bg-neutral-500 bg-opacity-10 rounded-lg font-body text-primary dark:text-white w-full flex justify-between items-center cursor-pointer"
               >
                <span class="flex flex-row items-center space-x-1 ">
                    Day of Week to Send On
                    
                  
                </span>
               
            </h4>

        <div class="grid grid-cols-3 sm:grid-cols-[repeat(auto-fit,minmax(80px,1fr))] my-2 w-full lg:w-1/2" x-show="schedule === 'Weekly'">
            @foreach (var day in Model.DaysOfWeek)
            {
                var checkedDay = day.ToString() == Model.Day ? "checked" : "";
                <div class="flex items-center w-auto h-full min-h-[50px]">
                    <input id="@day" type="radio" value="@day" name="day" @checkedDay class="hidden peer">
                    <label for="@day" class="ml-2 text-sm font-medium text-white 
            bg-blue-dark border-gray-light border rounded-xl px-1 py-2 w-full 
            peer-checked:text-blue-600 peer-checked:dark:bg-green text-center justify-center">
                        @day.ToString()
                    </label>
                </div>
            }
        </div>

            </div>
        <div :class="{ 'opacity-50 pointer-events-none': schedule !== 'Monthly' }" class=" mt-2 border-neutral-400 dark:border-neutral-600 border rounded-lg" >
            <h4
                class="px-5 py-1 bg-neutral-500 bg-opacity-10 rounded-lg font-body text-primary dark:text-white w-full flex justify-between items-center cursor-pointer">
                <span class="flex flex-row items-center space-x-1 ">
                    Day of Month to Send On
                </span>
            </h4>
            <div class="grid grid-cols-[repeat(auto-fit,minmax(35px,1fr))] w-full mx-2" x-show="schedule === 'Monthly'">
                @for(int i=1; i<32; i++)
                {
                    var checkedMonthDay = i == Model.DayOfMonth ? "checked" : "";
                    <div class="flex items-center w-auto my-2 h-full min-h-[35px]">
                        <input id="Day_@i" type="radio" value="@i" name="daypfmonth" @checkedMonthDay class="hidden peer">
                        <label for="Day_@i" class="ml-2 text-sm font-medium text-white 
            bg-blue-dark border-gray-light border rounded-xl px-1 py-2 w-full 
            peer-checked:text-blue-600 peer-checked:dark:bg-green text-center justify-center">
                             @i.GetOrdinal()
                        </label>
                    </div>
                }
            </div>
        </div>
        @* Action Buttons *@
        <div class="flex flex-row gap-2 mt-4">
            <button type="submit" class="btn btn-primary">Subscribe</button>
            <button type="reset" class="btn-warning btn">Reset</button>
        </div>
    </div>
</form>
```
</details>

You can see that this is pretty simple as it goes. It uses Alpine.js to handle all the user interactions and a common UI element for all selections.

```html
<div class="grid grid-cols-2 sm:grid-cols-[repeat(auto-fit,minmax(100px,1fr))] w-full lg:w-1/3">
 @{
var frequency = Enum.GetValues(typeof(SubscriptionType)).Cast<SubscriptionType>().ToList();
 }
 @foreach (var freq in frequency)
{
<div class="flex items-center w-auto h-full min-h-[30px] lg:mb-0 mb-3">
     <input x-model="schedule" id="@freq" type="radio" value="@freq.ToString()" name="SubscriptionType" class="hidden peer">
      <label for="@freq" class="ml-2 text-sm font-medium text-white 
                bg-blue-dark border-gray-light border rounded-xl px-1 py-2 w-full 
                peer-checked:text-blue-600 peer-checked:dark:bg-green text-center justify-center">
                            @freq.EnumDisplayName()
      </label>
</div>
}
</div>

```
You can see that this is CSS based, using the Tailwind CSS framework's `peer` CSS utility to specify that when the label is clicked on it should set the input's checked property and change it's styling.

I use this later in the page to determine which selector (Week Day / Day of Month) to make available to users & show the elements allowing selection.

```html
<div :class="{ 'opacity-50 pointer-events-none': schedule !== 'Monthly' }" class=" mt-2 border-neutral-400 dark:border-neutral-600 border rounded-lg" >
   <h4
       class="px-5 py-1 bg-neutral-500 bg-opacity-10 rounded-lg font-body text-primary dark:text-white w-full flex justify-between items-center cursor-pointer">
       <span class="flex flex-row items-center space-x-1 ">
           Day of Month to Send On
       </span>
   </h4>
   <div class="grid grid-cols-[repeat(auto-fit,minmax(35px,1fr))] w-full mx-2" x-show="schedule === 'Monthly'">
       @for(int i=1; i<32; i++)
       {
           var checkedMonthDay = i == Model.DayOfMonth ? "checked" : "";
           <div class="flex items-center w-auto my-2 h-full min-h-[35px]">
               <input id="Day_@i" type="radio" value="@i" name="daypfmonth" @checkedMonthDay class="hidden peer">
               <label for="Day_@i" class="ml-2 text-sm font-medium text-white 
   bg-blue-dark border-gray-light border rounded-xl px-1 py-2 w-full 
   peer-checked:text-blue-600 peer-checked:dark:bg-green text-center justify-center">
                    @i.GetOrdinal()
               </label>
           </div>
       }
   </div>
</div>
```
You can see that I have an Alpine.js css template class which sets the opacity of the element to 50% and disables pointer events if the schedule is not set to Monthly. This is a simple way to hide elements that are not needed.

```html
:class="{ 'opacity-50 pointer-events-none': schedule !== 'Monthly' }" 
```

It also hides the Monethly day selector if the schedule is not set to Monthly.

```html
x-show="schedule === 'Monthly'"
```
So that's the jist of the Schedule page. I'll cover the backend in the next post.

# Next Steps
In the next post I'll (when I finish bulding it) post the new structural changes to the project to allow me to use a separate web application for Hanfgire and the email sending service. This is a substantial change as I go from a single project `Mostlylucid` to a number of projects allowing sharing of services:
1. `Mostlylucid` - The main website project.

2. `Mostlylucid.SchedulerService` - This is the main Hangfire project which will acess the Database, build the emails and send them.

3. `Mostlylucid.Services` - Where the services live which return data to the top level projects

4. `Mostlylucid.Shared` - Helpers and Constants used by all projects.

5. `Mostlylucid.DbContext` - The database context for the project.

You can see this this adds significantly more complexity to the system architecture; but in this case it's necessary to keep the project maintainable and scalable.
I'll cover how I did this in the rest of this series.

# In Conclusion
I still have a TON of work to do to make all this happen. The refactoring is somewhat complex as it involves adding multiple layers to the system (and concepts like DTOs to the project) but I think it's worth it in the long run.