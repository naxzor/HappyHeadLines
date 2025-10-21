const regionSel   = document.querySelector('#region');
const reloadBtn   = document.querySelector('#reload');
const listEl      = document.querySelector('#list');
const focusEl     = document.querySelector('#focus');
const commentsEl  = document.querySelector('#comment-list');
const form        = document.querySelector('#comment-form');
const authorInp   = document.querySelector('#author');
const textInp     = document.querySelector('#text');

let currentArticle = null;

async function getJSON(url) {
  const r = await fetch(url, { headers: { 'accept': 'application/json' }});
  if (!r.ok) throw new Error(`HTTP ${r.status}`);
  return r.json();
}

function fmtDate(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  return d.toLocaleString();
}

async function loadArticles() {
  commentsEl.innerHTML = '';
  currentArticle = null;
  listEl.innerHTML = '<li><em>Loading…</em></li>';

  const region = regionSel.value;
  const data = await getJSON(`/v1/articles?regionScope=${encodeURIComponent(region)}&take=10&skip=0`);

  listEl.innerHTML = '';
  if (!data || data.length === 0) {
    focusEl.innerHTML = `<em>No articles yet for ${region}</em>`;
    return;
  }

  setFocus(data[0]);

  for (const a of data.slice(1)) {
    const li = document.createElement('li');
    li.innerHTML = `<strong>${a.title}</strong><br/><small>${a.language} • ${a.regionScope}</small>`;
    li.addEventListener('click', () => setFocus(a));
    listEl.appendChild(li);
  }
}

async function setFocus(a) {
  currentArticle = a;
  focusEl.innerHTML = `
    <h2>${a.title}</h2>
    <div class="meta">
      ${a.language} • ${a.regionScope} •
      Published: ${fmtDate(a.publishedAt)} •
      Updated: ${fmtDate(a.updatedAt)}
    </div>
    <div>${(a.body ?? '').replace(/\n/g, '<br/>')}</div>
  `;
  await loadComments(a.id);
}

async function loadComments(articleId) {
  commentsEl.innerHTML = '<li>Loading comments…</li>';
  try {
    const data = await getJSON(`/comments/article/${articleId}?take=20&skip=0`);
    commentsEl.innerHTML = '';
    if (!data || data.length === 0) {
      commentsEl.innerHTML = '<li><em>No comments yet.</em></li>';
      return;
    }
    for (const c of data) {
      const li = document.createElement('li');
      li.innerHTML = `<strong>${c.author}</strong> <small>${fmtDate(c.createdAt)}</small><br/>${c.body}`;
      commentsEl.appendChild(li);
    }
  } catch {
    commentsEl.innerHTML = '<li><em>Failed to load comments.</em></li>';
  }
}

form.addEventListener('submit', async (e) => {
  e.preventDefault();
  if (!currentArticle) return;

  const payload = {
    articleId: currentArticle.id,
    author:    authorInp.value.trim(),
    body:      textInp.value.trim()
  };
  if (!payload.author || !payload.body) return;

  try {
    const r = await fetch('/comments', {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify(payload)
    });

    if (!r.ok) {
      const err = await r.text().catch(() => '');
      throw new Error(`Failed to post: ${r.status} ${err}`);
    }

    authorInp.value = '';
    textInp.value   = '';
    await loadComments(currentArticle.id);
  } catch (err) {
    console.error(err);
    alert('Failed to post comment');
  }
});

reloadBtn.addEventListener('click', loadArticles);
regionSel.addEventListener('change', loadArticles);
document.addEventListener('DOMContentLoaded', loadArticles);
