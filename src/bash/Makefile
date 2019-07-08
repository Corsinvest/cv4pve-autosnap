PACKAGE=cv4pve-autosnap
VERSION=$(shell ./${PACKAGE} version)
DATE=$(shell LANG=en_us_8859_1; date '+%b %d, %Y')
DESTDIR=
PREFIX=/usr
SBINDIR=${PREFIX}/sbin
MANDIR=${PREFIX}/share/man
DOCDIR=${PREFIX}/share/doc/${PACKAGE}
MAN8DIR=${MANDIR}/man8
EXAMPLE=${DOCDIR}/examples

DEB=${PACKAGE}_${VERSION}_inside_pve.deb

all: ${DEB}

.PHONY: dinstall
dinstall: deb
	dpkg -i ${DEB}

.PHONY: install
install:
	install -d ${DESTDIR}${SBINDIR}
	install -m 0755 ${PACKAGE} ${DESTDIR}${SBINDIR}

	install -d ${DESTDIR}${MAN8DIR}
	install -m 0644 ${PACKAGE}.8 ${DESTDIR}${MAN8DIR}
	gzip -n -9 ${DESTDIR}${MAN8DIR}/${PACKAGE}.8

	install -d ${DESTDIR}${EXAMPLE}
	install -m 0755 script-hook.sh ${DESTDIR}${EXAMPLE}


.PHONY: deb ${DEB}
deb ${DEB}:
	rm -rf debian
	mkdir debian

	$(shell ./${PACKAGE} help --no-logo > help.tmp)
	sed '/@@COPYRIGHT@@/r copyright' ${PACKAGE}.8.template | \
	sed "/@@COPYRIGHT@@/d" | \
	sed '/@@SYNOPSIS@@/r help.tmp' | \
	sed /@@SYNOPSIS@@/d | \
	sed s/@@PACKAGE@@/${PACKAGE}/ |  \
	sed s/@@PACKAGE@@/${PACKAGE}/ |  \
	sed "s/@@DATE@@/${DATE}/"  \
	> ${PACKAGE}.8

	make DESTDIR=${CURDIR}/debian install
	install -d -m 0755 debian/DEBIAN
	sed -e s/@@VERSION@@/${VERSION}/ -e s/@@PACKAGE@@/${PACKAGE}/  <control.in >debian/DEBIAN/control
	install -D -m 0644 copyright debian/${DOCDIR}/copyright
	install -m 0644 changelog debian/${DOCDIR}/
	gzip -n -9 debian/${DOCDIR}/changelog
	dpkg-deb --build debian
	mv debian.deb ${DEB}
	#lintian ${DEB}
	rm -rf debian
	rm ${PACKAGE}.8
	rm help.tmp

.PHONY: clean
clean:
	rm -rf debian *.deb ${PACKAGE}-*.tar.gz dist *.8.man *.8.gz *.tmp
	find . -name '*~' -exec rm {} ';'

.PHONY: distclean
distclean: clean