# डॉकर बनाएं

<datetime class="hidden">2024- 26- 302: 30</datetime>

<!--category-- Docker -->
डॉकर निर्माण के लिए एक औजार है जो कि बहु- विभागी डॉकर अनुप्रयोग चला रहा है. सीडी के साथ, आप अपने अनुप्रयोग की सेवाओं को कॉन्फ़िगर करने के लिए YAएमएल फ़ाइल का उपयोग करते हैं. फिर एक एकल कमांड के साथ, आप अपने कॉन्फ़िगरेशन से सभी सेवाओं को प्रारंभ कर सकते हैं.

उस समय मैं अपने सर्वर पर कुछ सेवाओं को चलाने के लिए डॉकर का उपयोग करता हूँ.

- सबसे ज्यादा मेलेद - मेरा ब्लॉग (यह एक)
- बादलित - एक ऐसी सेवा जो मेरे सर्वर के यातायात को नष्ट करती है
- प्रहरीदुर्ग - एक सेवा जो मेरे बरतनों को अद्यतन करने की जाँच करती है और ज़रूरत पड़ने पर उन्हें फिर से चालू करती है ।

यहाँ है `docker-compose.yml` फ़ाइल मैं इन सेवाओं को चलाने के लिए इस्तेमाल करता हूँ:

```yaml
services:
  mostlylucid:
    image: scottgal/mostlylucid:latest
    labels:
        - "com.centurylinklabs.watchtower.enable=true"
  cloudflared:
    image: cloudflare/cloudflared:latest
    command: tunnel --no-autoupdate run --token ${CLOUDFLARED_TOKEN}
    env_file:
      - .env
        
  watchtower:
    image: containrrr/watchtower
    container_name: watchtower
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      - WATCHTOWER_CLEANUP=true
      - WATCHTOWER_LABEL_ENABLE=true
    command: --interval 300 # Check for updates every 300 seconds (5 minutes)
```