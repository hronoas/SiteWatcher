function click(s,t){
    t=(t||'xpath').toLowerCase();
    var e=t==='css'?document.querySelector(s):document.evaluate(s,document,null,9,null).singleNodeValue;
    if(e)e.dispatchEvent(new MouseEvent('click',{bubbles:true}));
}
const wait=(ms)=>new Promise(r=>setTimeout(r,ms));
