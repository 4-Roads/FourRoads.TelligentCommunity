function cs_setInnerHtml(id, html) {
  var e = document.getElementById(id);
  if (e)
    e.innerHTML = html;
}

function cs_openAndWriteToWindow(windowName, width, height, html) {
  if (!jQuery.telligent.glow) {
    alert('This content cannot be viewed in this context.');
    return;
  }

  var wi = jQuery.telligent.glow.utility.getWindowInfo();
  var s = jQuery('<div></div>');
  var p = jQuery('<div></div>');

  jQuery('body').append(p);
  p.glowPopUpPanel({ zIndex: 200003, position: 'downright', hideOnDocumentClick: false });
  p.glowPopUpPanel('html', '<div style="width:' + width + 'px;height:' + height + 'px;position:relative;color:#000;background-color:#fff;">' + html + '</div>');

  jQuery('body').append(s);
  s.glowPopUpPanel({ zIndex: 200002, position: 'downright', hideOnDocumentClick: false });
  s.glowPopUpPanel('html', '<div style="width:' + wi.ContentWidth + 'px;height:' + wi.ContentHeight + 'px;background-color: #000;opacity:.75"></div>');
  jQuery(s.glowPopUpPanel('children')).on('click', function () { p.glowPopUpPanel('hide'); s.glowPopUpPanel('hide'); p.remove(); s.remove(); return false; });
  s.glowPopUpPanel('show', wi.ScrollX, wi.ScrollY, wi.Width, 0);

  p.glowPopUpPanel('show', (width < wi.Width ? ((wi.Width - width) / 2) : 0) + wi.ScrollX, (height < wi.Height ? ((wi.Height - height) / 2) : 0) + wi.ScrollY, width, 0);
};