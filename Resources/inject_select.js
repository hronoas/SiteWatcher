let hoverClass='watch_hover';
let selectedClass='watch_selected';

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

function injectCSS(css) {
  head = document.head || document.getElementsByTagName('head')[0],
    style = document.createElement('style');
  head.appendChild(style);
  style.type = 'text/css';
  style.appendChild(document.createTextNode(css));
}

injectCSS( "."+hoverClass+",."+selectedClass+"{border:2px solid red; border-collpase:collapse; } \
."+selectedClass+"{border-color:blue;}")

function clearChoose(){
  document.querySelectorAll('.'+hoverClass).forEach(el=>el.classList.remove(hoverClass));
  document.querySelectorAll('.'+selectedClass).forEach(el=>el.classList.remove(selectedClass));
}
function selectChoose(selectors){
  if(!Array.isArray(selectors)) selectors = [selectors];
  clearChoose()
  selectors.forEach(sel=>{
    switch (sel.type.toLowerCase()) {
      case 'css':
        document.querySelectorAll(sel.value).forEach(el=>el.classList.add(selectedClass))
        break;
      case 'xpath':
        selectPath(sel.value).forEach(el=>el.classList.add(selectedClass))
        break;
      default:
        break;
    }
  })
  
}
selectChoose(parameters)