import {afterEach, beforeEach, describe, it, expect, vi} from 'vitest';
import '../explainer-embeds';

let frame;
beforeEach(()=>{
    document.documentElement.classList.remove('dark');document.body.classList.remove('dark');
    document.body.innerHTML='<iframe class="article-explainer" height="900"></iframe>';
    frame=document.querySelector('iframe');
    vi.spyOn(frame.contentWindow,'postMessage').mockImplementation(()=>{});
});
afterEach(()=>{document.body.replaceChildren();vi.restoreAllMocks();});
function size(height, origin=location.origin, source=frame.contentWindow){
    window.dispatchEvent(new MessageEvent('message',{origin,source,data:{type:'mostlylucid-explainer-size',height}}));
}
describe('article explainer boundary',()=>{
    it('sizes only a matching, same-origin child and bounds numeric input',()=>{
        size(1200,'https://unrelated.example');expect(frame.height).toBe('900');
        size(1200,location.origin,window);expect(frame.height).toBe('900');
        for(const invalid of [NaN,Infinity,299,2401,'invalid']){size(invalid);expect(frame.height).toBe('900');}
        size(1639);expect(frame.height).toBe('1639');size(1056);expect(frame.height).toBe('1056');
    });
    it('propagates theme changes on the root or body to embedded views',async()=>{
        const send=vi.spyOn(frame.contentWindow,'postMessage');
        document.body.classList.add('dark');
        await vi.waitFor(()=>expect(send).toHaveBeenCalledWith({type:'mostlylucid-explainer-theme',dark:true},location.origin));
        send.mockClear();document.body.classList.remove('dark');document.documentElement.classList.add('dark');
        await vi.waitFor(()=>expect(send).toHaveBeenCalledWith({type:'mostlylucid-explainer-theme',dark:true},location.origin));
        send.mockRestore();
    });
});
