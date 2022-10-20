function inRegexArr(str,arr){
  return arr.some((r)=>str.match(new RegExp(r,'gi')))
}
let hoverClass='watch_hover';
let selectedClass='watch_selected';

let allClasses = new Map();
let ignoreClass=['^[\\.\\-_]','__','\\-\\-','^'+hoverClass+'$','^'+selectedClass+'$']
document.querySelectorAll('*').forEach((el)=>{
  el.classList.forEach((cls)=>{
    if(cls && inRegexArr(cls,ignoreClass)){
      if (allClasses.has(cls)) allClasses.set(cls, 1 + allClasses.get(cls));
      else allClasses.set(cls, 1);
    }
  })
})

function getClass(el,asc=true) {
  let classname = ""
  return Array.from(el.classList).filter((x)=>!inRegexArr(x,ignoreClass)).sort((a,b)=>{
     (allClasses.get(a)??0-allClasses.get(b)??0)*(asc?1:-1);
  }).at(0)
}

let includeTag=['header','footer','address','aside', 'main', 'section', 'article', 'nav', 'h1', 'h2', 'h3']

function selectPath(path,parent) {
  let pth=path.replaceAll(/[\/]{2,}/g, '//')
  let results = [];
  let items = document.evaluate(pth, parent || document, null, 0, null);
  let item= items.iterateNext()
  while(item){
    results.push(item);
    item= items.iterateNext()
  }
  return results;
}

function elementToXpath(el,top=false){
  let tclass=getClass(el)
  if(!top && !tclass && !includeTag.includes(el.tagName.toLowerCase())) return "/";
  return el.tagName.toLowerCase()+ (tclass ? ('[contains(@class,"' + tclass + '")]') : "")
}


function getPathTo(element, next = '', depth = 15, top = true) {
  if (element.id && !element.id.match(new RegExp('[0-9]'))) return ('//' + element.tagName.toLowerCase() + "[@id='" + element.id + "']")
  if (depth == 0) return '/';
  if (element.tagName == 'HTML') return '/html';
  if (element === document.body) return '/html/body';

  let selector = elementToXpath(element,top)
  let nextpath = selector + (next ? '/' + next : '')
  if(selector=='/') return getPathTo(element.parentNode, nextpath, depth, false)+selector
  if (selectPath('//' + selector).length == 1) return ('//' + selector)
  let part = '//' + nextpath;
  let parents = []
  selectPath(part).some(el => {
    if (!parents.includes(el.parentNode)) parents.push(el.parentNode);
    return parents.length>1;
  })
  if (parents.length == 1) return ('//' + selector);

  let path = getPathTo(element.parentNode, nextpath, depth - 1, false) + '/' + selector;
  if (top) {
    path = path.replaceAll(/[\/]{2,}/g, '//');
    let selected = selectPath(path);
    if (selected.length > 1)
      for (let x = 0; x < selected.length; x++) {
        if (selected[x] == element) path = "(" + path + ")[position()=" + (x + 1) + "]"
      }
  }
  return path
}
function captureClick(e) {
  let el = e.target;
  e.preventDefault();
  endChoose();
  let path = getPathTo(el);
  console.log(path);
  if(typeof callback !== 'undefined')callback.send(path);
}

function captureMove(e) {
  let el = e.target;
  if (!el.classList.contains(hoverClass)) el.classList.add(hoverClass);
}

function uncaptureMove(e) {
  let el = e.target;
  if (el.classList.contains(hoverClass)) el.classList.remove(hoverClass);
}

function injectCSS(css) {
  head = document.head || document.getElementsByTagName('head')[0],
    style = document.createElement('style');
  head.appendChild(style);
  style.type = 'text/css';
  style.appendChild(document.createTextNode(css));
}

injectCSS( "."+hoverClass+",."+selectedClass+"{border:2px solid red; border-collpase:collapse; } \
."+selectedClass+"{border-color:blue;}")

function endChoose(){
  clearChoose()
  window.removeEventListener('click', captureClick, true);
  window.removeEventListener('mouseover', captureMove);
  window.removeEventListener('mouseout', uncaptureMove);  
}
function clearChoose(){
  document.querySelectorAll('.'+hoverClass).forEach(el=>el.classList.remove(hoverClass));
  document.querySelectorAll('.'+selectedClass).forEach(el=>el.classList.remove(selectedClass));
}
function beginChoose(){
  window.addEventListener('click', captureClick, true);
  window.addEventListener('mouseover', captureMove);
  window.addEventListener('mouseout', uncaptureMove);  
}
beginChoose()