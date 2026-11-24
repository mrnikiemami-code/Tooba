const { chromium } = require('playwright');
const path=require('path'); const fs=require('fs'); const {pathToFileURL}=require('url');
const file=process.argv[2], out=process.argv[3];
function lum(c){const [r,g,b]=c.map(x=>{x/=255;return x<=0.03928?x/12.92:Math.pow((x+0.055)/1.055,2.4)});return 0.2126*r+0.7152*g+0.0722*b;}
function ratio(a,b){const L1=lum(a),L2=lum(b);const hi=Math.max(L1,L2),lo=Math.min(L1,L2);return (hi+0.05)/(lo+0.05);}
function rgb(s){const m=s.match(/\d+/g).map(Number);return [m[0],m[1],m[2]];}
(async()=>{
  const b=await chromium.launch();
  const pg=await b.newPage({viewport:{width:1440,height:960},locale:'fa-IR'});
  await pg.goto(pathToFileURL(file).href,{waitUntil:'load'});
  const R={};
  // admin table screenshot
  await pg.evaluate(()=>switchTab('at')); await pg.waitForTimeout(200);
  await pg.screenshot({path:path.join(out,'r2-admin-table.png'),fullPage:true});
  // go form, create mode
  await pg.evaluate(()=>switchTab('af')); await pg.evaluate(()=>setMode('create')); await pg.waitForTimeout(200);
  R.depsLockedWhenEmpty = await pg.evaluate(()=>{const s=document.getElementById('sec-price');const dis=[...s.querySelectorAll('input,select,button')].every(e=>e.disabled||e.dataset.always);return s.classList.contains('dep-locked')||dis;});
  R.unitReadonly = await pg.evaluate(()=>{const inputs=[...document.querySelectorAll('#sec-qty input')];return inputs.filter(i=>i.disabled).length>=2;});
  R.warehouseLocked = await pg.evaluate(()=>document.getElementById('whName').classList.contains('locked'));
  R.historyHiddenOnCreate = await pg.evaluate(()=>getComputedStyle(document.getElementById('historyBlock')).display==='none' && getComputedStyle(document.getElementById('historyEmpty')).display!=='none');
  R.createNotBuyable = await pg.evaluate(()=>{const v=document.getElementById('statusSel').value;return v==='پیش‌نویس' && document.getElementById('buyable').value.includes('غیرقابل خرید');});
  await pg.screenshot({path:path.join(out,'r2-admin-form-create.png'),fullPage:true});
  // choose seller+product -> deps enable
  await pg.evaluate(()=>{choose('seller',{n:'فروشگاه آرمان',s:'تهران'});choose('product',{n:'گوشی هوشمند مدل X',b:'نمونه',v:'مشکی · ۱۲۸'});});
  await pg.waitForTimeout(150);
  R.depsEnabledAfterSelect = await pg.evaluate(()=>!document.getElementById('sec-price').classList.contains('dep-locked') && !document.getElementById('sec-price').querySelector('input,select').disabled);
  // responsibility unset -> service disabled
  R.serviceDisabledWhenUnset = await pg.evaluate(()=>document.getElementById('shipService').disabled===true);
  // set responsibility seller -> enabled
  await pg.evaluate(()=>{const b=document.querySelectorAll('#resp button')[0];setResp(b,'فروشنده');});
  R.serviceEnabledAfterResp = await pg.evaluate(()=>document.getElementById('shipService').disabled===false);
  // returns none -> window hidden
  await pg.evaluate(()=>document.querySelectorAll('#retSeg button')[2].click()); await pg.waitForTimeout(100);
  R.retWindowHiddenWhenNone = await pg.evaluate(()=>getComputedStyle(document.getElementById('retWindow')).display==='none');
  // draft not buyable / active buyable
  R.draftNotBuyable = await pg.evaluate(()=>{document.getElementById('statusSel').value='پیش‌نویس';setStatus();return document.getElementById('buyable').value.includes('غیرقابل خرید');});
  R.activeBuyable = await pg.evaluate(()=>{document.getElementById('statusSel').value='فعال';setStatus();return document.getElementById('buyable').value==='قابل خرید';});
  await pg.screenshot({path:path.join(out,'r2-admin-form-edit.png'),fullPage:true});
  // picker screenshot
  await pg.evaluate(()=>openPicker('product')); await pg.waitForTimeout(200);
  await pg.screenshot({path:path.join(out,'r2-product-picker.png')});
  await pg.evaluate(()=>closePicker());
  // overlap: sticky bar vs inputs (in af)
  R.overlap = await pg.evaluate(()=>{const bar=document.querySelector('#af .stickybar').getBoundingClientRect();const inputs=[...document.querySelectorAll('#af input,#af select')].filter(e=>{const r=e.getBoundingClientRect();return r.width>0&&r.height>0;});let bad=0;for(const e of inputs){const r=e.getBoundingClientRect();if(r.bottom>bar.top+1 && r.top<bar.bottom)bad++;}return bad;});
  // fonts & contrast
  R.minFont = await pg.evaluate(()=>{const els=[...document.querySelectorAll('#af label,#af td,#af button,#af .hint,#af .muted')];return Math.min(...els.map(e=>parseFloat(getComputedStyle(e).fontSize)));});
  R._bc=await pg.evaluate(()=>{const s=getComputedStyle(document.body);return [s.color,s.backgroundColor];});
  R.contrastBody=ratio(rgb(R._bc[0]),rgb(R._bc[1]));
  R._mc=await pg.evaluate(()=>{const e=document.querySelector('#af .muted');const s=getComputedStyle(e);return [s.color,getComputedStyle(document.body).backgroundColor];});
  R.contrastMuted=ratio(rgb(R._mc[0]),rgb(R._mc[1]));
  delete R._bc; delete R._mc;  // error state
  await pg.evaluate(()=>showErr()); await pg.waitForTimeout(100);
  R.errorShown = await pg.evaluate(()=>getComputedStyle(document.getElementById('errBox')).display!=='none');
  await pg.screenshot({path:path.join(out,'r2-admin-form-error.png'),fullPage:true});
  await pg.evaluate(()=>{document.getElementById('errBox').style.display='none';document.getElementById('sec-ship').classList.remove('errfield');});
  // seller
  await pg.evaluate(()=>switchTab('st')); await pg.waitForTimeout(150); await pg.screenshot({path:path.join(out,'r2-seller-table.png'),fullPage:true});
  await pg.evaluate(()=>switchTab('sf')); await pg.waitForTimeout(150); await pg.screenshot({path:path.join(out,'r2-seller-form.png'),fullPage:true});
  // mobile
  await pg.setViewportSize({width:360,height:740}); await pg.evaluate(()=>switchTab('af')); await pg.waitForTimeout(200);
  await pg.screenshot({path:path.join(out,'r2-mobile-admin-form.png'),fullPage:true});
  console.log(JSON.stringify(R,null,2));
  await b.close();
})().catch(e=>{console.error(e);process.exit(1);});

