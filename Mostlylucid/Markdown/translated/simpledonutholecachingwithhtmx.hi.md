# सिंपलेटेड होल का CTMMX के साथ सत्यापन करता है

# परिचय

डोट होल कलोन एक उपयोगी तकनीक हो सकती है जहाँ आप पृष्ठ के कुछ तत्वों को कैश करना चाहते हैं लेकिन सभी नहीं. लेकिन यह काम करने के लिए मुश्किल हो सकता है. इस पोस्ट में मैं आपको दिखाता हूँ कि कैसे एक सरल रफ़ल होल की तकनीक HMMX का उपयोग कर एक सरल रफ़िंग तकनीक को लागू करें।

<!--category-- HTMX, Razor, ASP.NET -->
<datetime class="hidden">2024- 09-1216: 00</datetime>
[विषय

# समस्या

एक कारण मैं इस साइट के साथ हो रहा था कि मैं अपने फ़ॉर्म के साथ Ant-Byyysyysysysysysys का उपयोग करना चाहता था. यह क्रॉस- एसआईआरी निवेदन को रोकने का एक अच्छा अभ्यास है हमले को रोकने के लिए. लेकिन, यह पन्‍नों के कवरिंग के साथ समस्या उत्पन्‍न कर रहा था । एन्टी- स्पैम टोकन प्रत्येक पृष्ठ निवेदन के लिए विशिष्ट है, अतः यदि आप पृष्ठ को कैश करते हैं, तो वह चिह्न सभी उपयोक्ताओं के लिए भी होगा. इसका अर्थ है कि यदि एक उपयोक्ता किसी रूप को स्वीकार करता है, तो चिन्ह अवैध होगा तथा फ़ॉर्म अधीनता असफल हो जाएगा. एनआईएस.NTC कोर इस निवेदन को अक्षम करने से रोकता है जहाँ एन्टी- स्पैम टोकन इस्तेमाल किया जाता है. यह एक अच्छी सुरक्षा अभ्यास है, लेकिन इसका मतलब है कि पृष्ठ किसी भी तरह से कैश नहीं किया जाएगा. यह ऐसी साइट के लिए आदर्श नहीं है जहाँ सामग्री ज़्यादातर स्थिर होती है ।

# हल

इस आसपास एक आम तरीका है 'नल्ड छेद' कलिंग जहाँ आप पृष्ठों के अधिकांश वितरित करते हैं, लेकिन कुछ तत्वों. इस तरह के बहुत सारे तरीके हैं जो एक-दूसरे से प्राप्त करने के लिए। वेर कोरी दृश्य फ्रेमवर्क का उपयोग कर रहा है हालांकि यह जटिल है और अक्सर विशिष्ट पैकेज और कॉन्फ़िग की आवश्यकता होती है। मैं एक आसान समाधान चाहता था.

जैसा कि मैं पहले से ही उत्तम उपयोग करता हूं [एचएमएमएक्स](https://htmx.org/examples/lazy-load/) इस परियोजना में एक बहुत ही सरल तरीका है कि AMMMMX के साथ आंशिक लोड कर दें।
मैं पहले से ही ब्लॉग कर चुका हूँ [जावा स्क्रिप्ट के साथ Antaks के लिए Antakeques प्रयोग कर रहा है](/blog/addingxsrfforjavascript) लेकिन यह मुद्दा फिर से था कि यह प्रभावकारी रूप से पृष्ठ के लिए कैशिंगिंग कर रहा था ।

अब मैं इस कार्य को बहाल कर सकते हैं जब HMMX का उपयोग गतिशील रूप से पक्षपाती लोड करने के लिए।

```razor
<li class="group relative mb-1">
    <div  hx-trigger="load" hx-get="/typeahead">
    </div>
</li>
```

मृत सरल, है ना? सभी यह नियंत्रण में कोड की एक पंक्ति में बुलाया जाता है जो आंशिक दृष्टिकोण लौटाता है. इसका अर्थ है कि सर्वर पर एन्टी- स्पैम टोकन तैयार किया जा सकता है तथा पृष्ठ सामान्य रूप से कैश किया जा सकता है. आंशिक दृष्टिकोण बहुत ही प्रभावशाली रूप से भरी हुई है इसलिए यह चिन्ह अब भी प्रत्येक निवेदन के लिए विशिष्ट है ।

```csharp
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpGet("typeahead")]
    public IActionResult TypeAhead()
    {
        return PartialView("_TypeAhead");
    }
```

आंशिक हम अभी भी के साथ सरल रूप है के साथ Antyyyymicioy टोकन के साथ.

```razor
<div x-data="window.mostlylucid.typeahead()" class="relative" id="searchelement"  x-on:click.outside="results = []">
    @Html.AntiForgeryToken()
    <label class="input input-sm dark:bg-custom-dark-bg bg-white input-bordered flex items-center gap-2">
        <input
            type="text"
            x-model="query"
            x-on:input.debounce.300ms="search"
            x-on:keydown.down.prevent="moveDown"
            x-on:keydown.up.prevent="moveUp"
            x-on:keydown.enter.prevent="selectHighlighted"
            placeholder="Search..."
            class="border-0 grow  input-sm text-black dark:text-white bg-transparent w-full"/>
        <i class="bx bx-search"></i>
    </label>
    <!-- Dropdown -->
    <ul x-show="results.length > 0"
        class="absolute z-10 my-2 w-full bg-white dark:bg-custom-dark-bg border border-1 text-black dark:text-white border-b-neutral-600 dark:border-gray-300   rounded-lg shadow-lg">
        <template x-for="(result, index) in results" :key="result.slug">
            <li
                x-on:click="selectResult(result)"
                :class="{
                    'dark:bg-blue-dark bg-blue-light': index === highlightedIndex,
                    'dark:hover:bg-blue-dark hover:bg-blue-light': true
                }"
                class="cursor-pointer text-sm p-2 m-2"
                x-text="result.title"
            ></li>
        </template>
    </ul>
</div>
```

फिर यह Spaplys सभी कोड प्रकार की खोज के लिए और जब यह जमा कर दिया है वह चिह्न खींच लेता है और यह अनुरोध में जोड़ देता है (जैसे कि पहले के रूप में).

```javascript
        let token = document.querySelector('#searchelement input[name="__RequestVerificationToken"]').value;
            console.log(token);
            fetch(`/api/search/${encodeURIComponent(this.query)}`, { // Fixed the backtick and closing bracket
                method: 'GET', // or 'POST' depending on your needs
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': token // Attach the AntiForgery token in the headers
                }
            })
```

# ऑन्टियम

यह HMMMX के साथ 'नृतल छेद' प्राप्त करने के लिए एक सुपर सरल तरीका है. यह एक अतिरिक्त पैकेज की जटिलता के बिना सीलिंग के लाभ प्राप्त करने का महान तरीका है. मुझे आशा है कि आप यह उपयोगी पाते हैं. मुझे पता है कि क्या आप नीचे टिप्पणी में किसी भी सवाल है।